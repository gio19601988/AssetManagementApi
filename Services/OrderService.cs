// Services/OrderService.cs
using AssetManagementApi.Controllers;
using AssetManagementApi.Data;
using AssetManagementApi.Dtos.Orders;
using AssetManagementApi.Exceptions;
using AssetManagementApi.Models.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Security.Claims;  // User ID-ისთვის JWT-დან
using MailKit.Net.Smtp;
using MimeKit;
using MailKit.Security;  // SecureSocketOptions-ისთვის

namespace AssetManagementApi.Services
{
    public class OrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;  // მიმდინარე მომხმარებლის ინფოსთვის
        private readonly ILogger<OrderService> _logger;

        public OrderService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, ILogger<OrderService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        // ────────────────────────────────────────────────────────────────
        // შეკვეთების სია (ფილტრაციით, RBAC შემოწმებით)
        // ────────────────────────────────────────────────────────────────
        public async Task<List<OrderDto>> GetOrdersAsync(string? statusCode = null, int? requesterId = null)
        {
            var userId = GetCurrentUserId();  // მიმდინარე მომხმარებლის ID

            var query = _context.Orders
                .Include(o => o.Status)
                .Include(o => o.OrderType)
                .Include(o => o.Items)
                .Include(o => o.Documents)
                .Include(o => o.Comments)
                .AsQueryable();

            if (statusCode != null)
                query = query.Where(o => o.Status != null && o.Status.Code == statusCode);

            if (requesterId != null)
                query = query.Where(o => o.RequesterId == requesterId.Value);

            // RBAC: თუ არა admin, მხოლოდ საკუთარი შეკვეთები
            if (!await HasPermissionAsync("orders.view.all"))
                query = query.Where(o => o.RequesterId == userId || o.CreatedBy == userId);

            var orders = await query.ToListAsync();

            return orders.Select(o => MapToDto(o)).ToList();
        }


        //ახალი დამატებული მეთოდები სტატუსებისა და ტიპების მისაღებად
        public async Task<List<OrderStatusDto>> GetOrderStatusesAsync()
        {
            return await _context.OrderStatuses
                .Where(s => s.IsActive)
                .OrderBy(s => s.OrderSeq)
                .Select(s => new OrderStatusDto
                {
                    Id = s.Id,
                    Code = s.Code,
                    NameKa = s.NameKa,
                    Color = s.Color,
                    OrderSeq = s.OrderSeq,
                    IsActive = s.IsActive
                })
                .ToListAsync();
        }

        public async Task<List<OrderTypeDto>> GetOrderTypesAsync()
        {
            return await _context.OrderTypes
                .Where(t => t.IsActive)
                .Select(t => new OrderTypeDto
                {
                    Id = t.Id,
                    Code = t.Code,
                    NameKa = t.NameKa,
                    RequiresApproval = t.RequiresApproval,
                    ApprovalLevels = t.ApprovalLevels
                })
                .ToListAsync();
        }

        // ────────────────────────────────────────────────────────────────
        // კონკრეტული შეკვეთის მიღება ID-ით
        // ────────────────────────────────────────────────────────────────
        public async Task<OrderDto> GetOrderByIdAsync(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Status)
                .Include(o => o.OrderType)
                .Include(o => o.Items)
                .Include(o => o.Documents)      // ← ეს აუცილებელია
                .Include(o => o.Comments)       // ← ეს აუცილებელია
                .Include(o => o.WorkflowHistories)  // ← თუ გაქვს WorkflowHistories navigation
                .ThenInclude(w => w.FromStatus)     // ← optional, სახელებისთვის
                                                    //.ThenInclude(w => w.ToStatus)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                throw new NotFoundException("შეკვეთა არ მოიძებნა");

            // RBAC შემოწმება
            if (!await HasPermissionAsync("orders.view.all") && order.RequesterId != GetCurrentUserId())
                throw new UnauthorizedAccessException("უფლება არ გაქვთ ამ შეკვეთის ნახვაზე");

            return MapToDto(order);
        }

        // ────────────────────────────────────────────────────────────────
        // ახალი შეკვეთის შექმნა
        // ────────────────────────────────────────────────────────────────
        public async Task<OrderDto> CreateOrderAsync(CreateOrderDto dto)
        {
            if (!await HasPermissionAsync("orders.create"))
                throw new UnauthorizedAccessException("უფლება არ გაქვთ შეკვეთის შექმნაზე");

            var userId = GetCurrentUserId();

            var order = new Order
            {
                OrderNumber = GenerateOrderNumber(),
                OrderTypeId = dto.OrderTypeId,
                StatusId = await GetStatusIdAsync("pending"),
                RequesterId = userId,
                Title = dto.Title,
                Description = dto.Description,
                Priority = dto.Priority,
                EstimatedAmount = dto.EstimatedAmount,
                Currency = dto.Currency,
                RequestedDate = dto.RequestedDate,
                RequiredByDate = dto.RequiredByDate,
                DepartmentId = dto.DepartmentId,
                CreatedBy = userId,
                Items = dto.Items?.Select(i => new OrderItem
                {
                    AssetId = i.AssetId,
                    ItemName = i.ItemName,
                    ItemDescription = i.ItemDescription,
                    CategoryId = i.CategoryId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.TotalPrice,
                    Notes = i.Notes
                }).ToList() ?? new List<OrderItem>()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Workflow ისტორიის დამატება
            await AddWorkflowHistory(order.Id, null, order.StatusId, "შეკვეთა შეიქმნა", userId);

            return MapToDto(order);
        }

        // ────────────────────────────────────────────────────────────────
        // შეკვეთის განახლება
        // ────────────────────────────────────────────────────────────────
        public async Task<OrderDto> UpdateOrderAsync(int id, UpdateOrderDto dto)
        {
            var order = await _context.Orders.FindAsync(id)
                ?? throw new NotFoundException("შეკვეთა არ მოიძებნა");

            var userId = GetCurrentUserId();
            if (!await HasPermissionAsync("orders.edit.all") && order.RequesterId != userId)
                throw new UnauthorizedAccessException("უფლება არ გაქვთ რედაქტირებაზე");

            if (dto.Title != null) order.Title = dto.Title;
            if (dto.Description != null) order.Description = dto.Description;
            if (dto.Priority != null) order.Priority = dto.Priority;
            if (dto.EstimatedAmount != null) order.EstimatedAmount = dto.EstimatedAmount;
            if (dto.Currency != null) order.Currency = dto.Currency;
            if (dto.RequestedDate != null) order.RequestedDate = dto.RequestedDate.Value;
            if (dto.RequiredByDate != null) order.RequiredByDate = dto.RequiredByDate.Value;
            if (dto.DepartmentId != null) order.DepartmentId = dto.DepartmentId;
            if (dto.OrderTypeId != null) order.OrderTypeId = dto.OrderTypeId.Value;

            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapToDto(order);
        }

        // ────────────────────────────────────────────────────────────────
        // შეკვეთის წაშლა
        // ────────────────────────────────────────────────────────────────
        public async Task DeleteOrderAsync(int id)
        {
            var order = await _context.Orders.FindAsync(id) ?? throw new NotFoundException("შეკვეთა არ მოიძებნა");

            if (!await HasPermissionAsync("orders.delete"))
                throw new UnauthorizedAccessException("უფლება არ გაქვთ წაშლაზე");

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
        }

        // ────────────────────────────────────────────────────────────────
        // სტატუსის ცვლილება (მაგ. submit, approve, reject, complete, cancel)
        // ────────────────────────────────────────────────────────────────
        public async Task<OrderDto> ChangeStatusAsync(int id, string newStatusCode, string? comments = null)
        {
            var order = await _context.Orders.Include(o => o.Status).FirstOrDefaultAsync(o => o.Id == id) ?? throw new NotFoundException("შეკვეთა არ მოიძებნა");

            var permission = GetRequiredPermissionForStatus(newStatusCode);
            if (!await HasPermissionAsync("Orders.approval"))
                throw new UnauthorizedAccessException("უფლება არ გაქვთ სტატუსის ცვლილებაზე");

            var newStatusId = await GetStatusIdAsync(newStatusCode);
            if (newStatusId == 0) throw new InvalidOperationException("სტატუსი არ მოიძებნა");

            if (!IsValidTransition(order.Status?.Code ?? string.Empty, newStatusCode))
                throw new InvalidOperationException("სტატუსის ცვლილება არ არის დაშვებული");

            var userId = GetCurrentUserId();
            await AddWorkflowHistory(id, order.StatusId, newStatusId, comments, userId);

            order.StatusId = newStatusId;
            order.UpdatedAt = DateTime.UtcNow;
            if (newStatusCode == "approved") order.ApprovedDate = DateTime.UtcNow;
            if (newStatusCode == "completed") order.CompletedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapToDto(order);
        }

        // ────────────────────────────────────────────────────────────────
        // Workflow ისტორიის მიღება
        // ────────────────────────────────────────────────────────────────
        public async Task<List<OrderWorkflowDto>> GetWorkflowHistoryAsync(int id)
        {
            var history = await _context.OrderWorkflows
                .Where(w => w.OrderId == id)
                .Include(w => w.FromStatus)
                .Include(w => w.ToStatus)
                .OrderBy(w => w.ChangedAt)
                .ToListAsync();

            return history.Select(w => new OrderWorkflowDto
            {
                Id = w.Id,
                FromStatusNameKa = w.FromStatus?.NameKa ?? string.Empty,
                ToStatusNameKa = w.ToStatus?.NameKa ?? string.Empty,
                ChangedBy = w.ChangedBy,
                ChangedByName = _context.AppUsers.Find(w.ChangedBy)?.FullName ?? string.Empty,
                ChangedAt = w.ChangedAt,
                Comments = w.Comments,
                Metadata = w.Metadata
            }).ToList();
        }

        // ────────────────────────────────────────────────────────────────
        // კომენტარების მიღება
        // ────────────────────────────────────────────────────────────────
        public async Task<List<OrderCommentDto>> GetCommentsAsync(int id)
        {
            var comments = await _context.OrderComments
                .Where(c => c.OrderId == id)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            return comments.Select(c => new OrderCommentDto
            {
                Id = c.Id,
                UserId = c.UserId,
                UserName = _context.AppUsers.Find(c.UserId)?.FullName ?? string.Empty,
                Comment = c.Comment,
                IsInternal = c.IsInternal,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            }).ToList();
        }

        // ────────────────────────────────────────────────────────────────
        // ახალი კომენტარის დამატება
        // ────────────────────────────────────────────────────────────────
        public async Task<OrderCommentDto> AddCommentAsync(int id, CreateOrderCommentDto dto)
        {
            var order = await _context.Orders.FindAsync(id) ?? throw new NotFoundException("შეკვეთა არ მოიძებნა");

            var userId = GetCurrentUserId();

            var comment = new OrderComment
            {
                OrderId = id,
                UserId = userId,
                Comment = dto.Comment,
                IsInternal = dto.IsInternal
            };

            _context.OrderComments.Add(comment);
            await _context.SaveChangesAsync();

            return new OrderCommentDto
            {
                Id = comment.Id,
                UserId = comment.UserId,
                UserName = _context.AppUsers.Find(comment.UserId)?.FullName ?? string.Empty,
                Comment = comment.Comment,
                IsInternal = comment.IsInternal,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt
            };
        }

        // ────────────────────────────────────────────────────────────────
        // კომენტარის განახლება
        // ────────────────────────────────────────────────────────────────
        public async Task<OrderCommentDto> UpdateCommentAsync(int id, int commentId, UpdateOrderCommentDto dto)
        {
            var comment = await _context.OrderComments.FirstOrDefaultAsync(c => c.OrderId == id && c.Id == commentId) ?? throw new NotFoundException("კომენტარი არ მოიძებნა");

            var userId = GetCurrentUserId();
            if (comment.UserId != userId && !await HasPermissionAsync("orders.edit.all"))
                throw new UnauthorizedAccessException("უფლება არ გაქვთ კომენტარის განახლებაზე");

            comment.Comment = dto.Comment;
            if (dto.IsInternal.HasValue) comment.IsInternal = dto.IsInternal.Value;
            comment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new OrderCommentDto
            {
                Id = comment.Id,
                UserId = comment.UserId,
                UserName = _context.AppUsers.Find(comment.UserId)?.FullName ?? string.Empty,
                Comment = comment.Comment,
                IsInternal = comment.IsInternal,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt
            };
        }

        // ────────────────────────────────────────────────────────────────
        // კომენტარის წაშლა
        // ────────────────────────────────────────────────────────────────
        public async Task DeleteCommentAsync(int id, int commentId)
        {
            var comment = await _context.OrderComments.FirstOrDefaultAsync(c => c.OrderId == id && c.Id == commentId) ?? throw new NotFoundException("კომენტარი არ მოიძებნა");

            var userId = GetCurrentUserId();
            if (comment.UserId != userId && !await HasPermissionAsync("orders.delete"))
                throw new UnauthorizedAccessException("უფლება არ გაქვთ კომენტარის წაშლაზე");

            _context.OrderComments.Remove(comment);
            await _context.SaveChangesAsync();
        }

        // ────────────────────────────────────────────────────────────────
        // დოკუმენტის ატვირთვა
        // ────────────────────────────────────────────────────────────────
        // 1. დოკუმენტის ატვირთვა
        public async Task<OrderDocumentDto> UploadDocumentAsync(int orderId, IFormFile file, string? description)
        {
            var order = await _context.Orders.FindAsync(orderId)
                ?? throw new NotFoundException("შეკვეთა არ მოიძებნა");

            if (file == null || file.Length == 0)
                throw new ArgumentException("ფაილი არ არის მითითებული");

            var userId = GetCurrentUserId();

            // ფაილის შენახვა wwwroot/uploads/orders/{orderId}/
            var uploadDir = Path.Combine("wwwroot", "uploads", "orders", orderId.ToString());
            Directory.CreateDirectory(uploadDir);

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadDir, fileName);
            var relativePath = $"/uploads/orders/{orderId}/{fileName}";

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var document = new OrderDocument
            {
                OrderId = orderId,
                FileName = file.FileName,
                FilePath = relativePath,
                FileSize = file.Length,
                FileType = file.ContentType,
                UploadedBy = userId,
                UploadedAt = DateTime.UtcNow,
                Description = description
            };

            _context.OrderDocuments.Add(document);
            await _context.SaveChangesAsync();

            return new OrderDocumentDto
            {
                Id = document.Id,
                FileName = document.FileName,
                FilePath = document.FilePath,
                FileSize = document.FileSize,
                FileType = document.FileType,
                UploadedBy = userId,
                UploadedAt = document.UploadedAt,
                Description = document.Description
            };
        }


        // ახალი: ნივთების ძებნა წინა შეკვეთებში
        public async Task<List<OrderItemDto>> SearchOrderItemsAsync(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return new List<OrderItemDto>();

            return await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.ItemName.Contains(q))  // ან დაამატე სხვა კრიტერიუმები (e.g. oi.Order.OrderNumber.Contains(q))
                .Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    OrderId = oi.OrderId,
                    ItemName = oi.ItemName,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    TotalPrice = oi.TotalPrice

                })
                .Take(50)  // ლიმიტი
                .ToListAsync();
        }

        // ────────────────────────────────────────────────────────────────
        // დოკუმენტების მიღება
        // ────────────────────────────────────────────────────────────────
        public async Task<List<OrderDocumentDto>> GetDocumentsAsync(int id)
        {
            var documents = await _context.OrderDocuments
                .Where(d => d.OrderId == id)
                .ToListAsync();

            return documents.Select(d => new OrderDocumentDto
            {
                Id = d.Id,
                FileName = d.FileName,
                FilePath = d.FilePath,
                FileSize = d.FileSize,
                FileType = d.FileType,
                UploadedBy = d.UploadedBy,
                UploadedByName = _context.AppUsers.Find(d.UploadedBy)?.FullName ?? string.Empty,
                UploadedAt = d.UploadedAt,
                Description = d.Description
            }).ToList();
        }

        // ────────────────────────────────────────────────────────────────
        // დოკუმენტის განახლება (მაგ. description)
        // ────────────────────────────────────────────────────────────────
        public async Task<OrderDocumentDto> UpdateDocumentAsync(int orderId, int docId, UpdateDocumentDto dto)
        {
            var document = await _context.OrderDocuments
                .FirstOrDefaultAsync(d => d.OrderId == orderId && d.Id == docId)
                ?? throw new NotFoundException("დოკუმენტი არ მოიძებნა");

            var userId = GetCurrentUserId();
            if (document.UploadedBy != userId && !await HasPermissionAsync("orders.edit.all"))
                throw new UnauthorizedAccessException("უფლება არ გაქვთ");

            if (dto.Description != null) document.Description = dto.Description;
            // თუ გინდა სახელის რედაქტირება — დაამატე FileName-იც

            document.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new OrderDocumentDto
            {
                Id = document.Id,
                FileName = document.FileName,
                FilePath = document.FilePath,
                FileSize = document.FileSize,
                FileType = document.FileType,
                UploadedBy = document.UploadedBy,
                UploadedAt = document.UploadedAt,
                Description = document.Description
            };
        }

        // ────────────────────────────────────────────────────────────────
        // დოკუმენტის წაშლა (ფაილის წაშლით)
        // ────────────────────────────────────────────────────────────────
        // 2. დოკუმენტის წაშლა
        public async Task DeleteDocumentAsync(int orderId, int documentId)
        {
            var document = await _context.OrderDocuments
                .FirstOrDefaultAsync(d => d.OrderId == orderId && d.Id == documentId)
                ?? throw new NotFoundException("დოკუმენტი არ მოიძებნა");

            var userId = GetCurrentUserId();
            if (document.UploadedBy != userId && !await HasPermissionAsync("orders.delete"))
                throw new UnauthorizedAccessException("უფლება არ გაქვთ");

            var fullPath = Path.Combine("wwwroot", document.FilePath.TrimStart('/'));
            if (File.Exists(fullPath))
                File.Delete(fullPath);

            _context.OrderDocuments.Remove(document);
            await _context.SaveChangesAsync();
        }

        // ────────────────────────────────────────────────────────────────
        // Helper მეთოდები (უცვლელი)
        // ────────────────────────────────────────────────────────────────
        private int GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            return int.Parse(userIdClaim?.Value ?? throw new UnauthorizedAccessException("მომხმარებელი არ არის აუთენტიფიცირებული"));
        }

        private async Task<bool> HasPermissionAsync(string permissionCode)
        {
            var userId = GetCurrentUserId();
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .SelectMany(ur => ur.Role.RolePermissions)
                .AnyAsync(rp => rp.Permission.Code == permissionCode);
        }

        private string GenerateOrderNumber()
        {
            var year = DateTime.UtcNow.Year;
            var count = _context.Orders.Count(o => o.CreatedAt.Year == year) + 1;
            return $"ORD-{year}-{count:000}";
        }

        private async Task<int> GetStatusIdAsync(string code)
        {
            return await _context.OrderStatuses
                .Where(s => s.Code == code)
                .Select(s => s.Id)
                .FirstOrDefaultAsync();
        }

        private bool IsValidTransition(string fromCode, string toCode)
        {
            var allowed = new Dictionary<string, List<string>>
    {
        {"pending", new List<string> {"review", "cancelled", "archived"}},
        {"review", new List<string> {"approved", "rejected", "pending", "archived"}},
        {"approved", new List<string> {"completed", "cancelled", "archived"}},
        {"completed", new List<string> {"archived"}},          // ← დასრულებულიდან შეიძლება არქივი
        {"cancelled", new List<string> {"archived"}},         // ← გაუქმებულიდან შეიძლება არქივი
        {"rejected", new List<string> {"pending", "archived"}},
        {"archived", new List<string> {}}                     // არქივირებულიდან არსად
    };

            return allowed.TryGetValue(fromCode.ToLower(), out var transitions)
                && transitions?.Contains(toCode.ToLower()) == true;
        }

        private string GetRequiredPermissionForStatus(string statusCode)
        {
            var permissions = new Dictionary<string, string>
            {
                {"review", "orders.view.all"},
                {"approved", "orders.approve"},
                {"rejected", "orders.approve"},
                {"completed", "orders.complete"},
                {"cancelled", "orders.cancel"},
                {"archived", "orders.archive"},
                {"pending", "orders.edit.all"},
                {"default", "orders.edit.all"}
            };

            return permissions.GetValueOrDefault(statusCode, "orders.edit.all");
        }

        private async Task AddWorkflowHistory(int orderId, int? fromStatusId, int toStatusId, string? comments, int userId)
        {
            _context.OrderWorkflows.Add(new OrderWorkflow
            {
                OrderId = orderId,
                FromStatusId = fromStatusId,
                ToStatusId = toStatusId,
                ChangedBy = userId,
                Comments = comments
            });
            await _context.SaveChangesAsync();
        }

        private OrderDto MapToDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                Title = order.Title,
                Description = order.Description,
                Priority = order.Priority,
                EstimatedAmount = order.EstimatedAmount,
                Currency = order.Currency,
                RequestedDate = order.RequestedDate,
                RequiredByDate = order.RequiredByDate,
                ApprovedDate = order.ApprovedDate,
                CompletedDate = order.CompletedDate,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                StatusNameKa = order.Status?.NameKa ?? string.Empty,
                TypeNameKa = order.OrderType?.NameKa ?? string.Empty,
                RequesterId = order.RequesterId,
                RequesterName = _context.AppUsers.Find(order.RequesterId)?.FullName ?? string.Empty,
                Items = order.Items?.Select(i => new OrderItemDto
                {
                    Id = i.Id,
                    ItemName = i.ItemName,
                    ItemDescription = i.ItemDescription,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.TotalPrice
                }).ToList() ?? new List<OrderItemDto>(),
                Documents = order.Documents?.Select(d => new OrderDocumentDto
                {
                    Id = d.Id,
                    FileName = d.FileName,
                    FilePath = d.FilePath,
                    FileSize = d.FileSize,
                    FileType = d.FileType,
                    UploadedBy = d.UploadedBy,
                    UploadedByName = _context.AppUsers.Find(d.UploadedBy)?.FullName ?? string.Empty,
                    UploadedAt = d.UploadedAt,
                    Description = d.Description
                }).ToList() ?? new List<OrderDocumentDto>(),

                Comments = order.Comments?.Select(c => new OrderCommentDto
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    UserName = _context.AppUsers.Find(c.UserId)?.FullName ?? string.Empty,
                    Comment = c.Comment,
                    IsInternal = c.IsInternal,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                }).ToList() ?? new List<OrderCommentDto>(),

                WorkflowHistory = order.WorkflowHistories?.Select(w => new WorkflowHistoryDto
                {
                    FromStatusNameKa = w.FromStatus?.NameKa,
                    ToStatusNameKa = w.ToStatus?.NameKa ?? "უცნობი",
                    ChangedByName = _context.AppUsers.Find(w.ChangedBy)?.FullName ?? "სისტემა",
                    ChangedAt = w.ChangedAt,
                    Comments = w.Comments
                }).ToList() ?? new List<WorkflowHistoryDto>()
            };
        }

        public async Task SendNewOrderNotification(Order order)
        {
            // იპოვე დამამტკიცებლები (მაგ. Role = "approver")
            var approvers = await (from u in _context.AppUsers
                                   join ur in _context.UserRoles on u.Id equals ur.UserId
                                   join r in _context.Roles on ur.RoleId equals r.Id
                                   where r.Name.ToLower() == "approver"
                                   select u.Email)
                               .Distinct()
                               .ToListAsync();

            if (!approvers.Any())
            {
                _logger.LogWarning("No approvers found for order #{OrderNumber}. No email sent.", order.OrderNumber);
                return;
            }
            _logger.LogInformation("Preparing to send notification for order #{OrderNumber} to {Count} approvers: {Emails}",
                order.OrderNumber, approvers.Count, string.Join(", ", approvers));

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Asset Management System", "giorobaqidze88@gmail.com"));

            foreach (var email in approvers)
            {
                message.To.Add(new MailboxAddress("", email));
                _logger.LogDebug("Added recipient: {Email}", email);
            }

            message.Subject = $"ახალი შეკვეთა შეიქმნა: #{order.OrderNumber}";

            var builder = new BodyBuilder
            {
                TextBody = $"""
            გამარჯობა,

            ახალი შეკვეთა შეიქმნა:
            შეკვეთის №: {order.OrderNumber}
            სათაური: {order.Title}
            მოთხოვნა: {order.RequesterId} (სრული სახელი თუ გაქვს)
            თარიღი: {order.RequestedDate:dd.MM.yyyy}

            გადადით ლინკზე დეტალების სანახავად:
            http://localhost:5173/orders/{order.Id}

            მადლობა!
            """
            };

            message.Body = builder.ToMessageBody();

            try
            {
                _logger.LogInformation("Connecting to SMTP server...");
                using var client = new SmtpClient();
                await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                _logger.LogInformation("Connected to SMTP. Authenticating...");

                // შენი მონაცემები
                await client.AuthenticateAsync("giorobaqidze88@gmail.com", "hldu utqx hkcf asbh");
                _logger.LogInformation("Authenticated successfully. Sending email...");

                await client.SendAsync(message);
                _logger.LogInformation("Email successfully sent to {Count} recipients for order #{OrderNumber}", approvers.Count, order.OrderNumber);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email notification sent to {Count} approvers for order #{OrderNumber}", approvers.Count, order.OrderNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email for order #{OrderNumber}. Error: {Message}", order.OrderNumber, ex.Message);
                throw; // ← ეს გადავცემთ კონტროლერს, რომ frontend-მა დაინახოს error
            }
        }
    }
}