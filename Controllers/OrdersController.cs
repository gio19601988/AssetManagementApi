using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssetManagementApi.Data;
using AssetManagementApi.Dtos.Orders;
using AssetManagementApi.Models.Orders;
using AssetManagementApi.Models;
using AssetManagementApi.Exceptions; // ← ეს აუცილებელია NotFoundException-ისთვის
using System.Security.Claims;
using Microsoft.AspNetCore.Http; // IFormFile-ისთვის
using System.IO;
using AssetManagementApi.Services;  // ← ეს დაამატე ზემოთ, სადაც სხვა using-ებია

namespace AssetManagementApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly OrderService _orderService;          // ← ეს დაამატე
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(ApplicationDbContext context, OrderService orderService, ILogger<OrdersController> logger)
    {
        _context = context;
        _orderService = orderService;
        _logger = logger;
    }

    // GET: api/orders
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetOrders(
        [FromQuery] string? status = null,
        [FromQuery] int? requesterId = null)
    {
        var query = _context.Orders
            .Include(o => o.Status)
            .Include(o => o.OrderType)
            .Include(o => o.Items)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(o => o.Status != null && o.Status.Code == status);

        if (requesterId.HasValue)
            query = query.Where(o => o.RequesterId == requesterId.Value);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.Title,
                o.Description,
                o.Priority,
                StatusCode = o.Status != null ? o.Status.Code : "unknown",
                StatusNameKa = o.Status != null ? o.Status.NameKa : "უცნობი",
                o.RequesterId,
                RequesterName = "უცნობი მომხმარებელი",
                o.DepartmentId,
                o.EstimatedAmount,
                o.Currency,
                o.RequestedDate,
                o.RequiredByDate,
                o.CreatedAt,
                ItemsCount = o.Items.Count
            })
            .ToListAsync();

        return Ok(orders);
    }

    // GET: api/orders/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDetailDto>> GetOrder(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Status)
            .Include(o => o.OrderType)
            .Include(o => o.Items)
            .Include(o => o.Documents)
            .Include(o => o.Comments)
            .Include(o => o.WorkflowHistories)
                .ThenInclude(w => w.FromStatus)  // მხოლოდ FromStatus, ToStatus-ის პრობლემა მოგვარებულია
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound(new { message = "შეკვეთა არ მოიძებნა" });

        // Requester-ის და Department-ის მონაცემები ცალკე query-ით (სწრაფი და სუფთა)
        var requester = await _context.AppUsers.FindAsync(order.RequesterId);
        var department = order.DepartmentId.HasValue
            ? await _context.Departments.FindAsync(order.DepartmentId.Value)
            : null;

        var dto = new OrderDetailDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            OrderTypeId = order.OrderTypeId,
            OrderTypeName = order.OrderType?.Name,
            OrderTypeNameKa = order.OrderType?.NameKa,
            StatusId = order.StatusId,
            StatusCode = order.Status?.Code ?? "unknown",
            StatusName = order.Status?.Name ?? "Unknown",
            StatusNameKa = order.Status?.NameKa ?? "უცნობი",
            RequesterId = order.RequesterId,
            RequesterName = requester?.Username ?? requester?.FullName ?? "უცნობი მომხმარებელი", // ← Username დამატებულია
            DepartmentId = order.DepartmentId,
            DepartmentName = department?.Name ?? null,
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

            // სრული Items fields (ყველა შესაძლო ველი)
            Items = order.Items.Select(i => new OrderItemDto
            {
                Id = i.Id,
                OrderId = i.OrderId,
                AssetId = i.AssetId,
                ItemName = i.ItemName,
                ItemDescription = i.ItemDescription,
                CategoryId = i.CategoryId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice,
                Notes = i.Notes,
                CreatedAt = i.CreatedAt
            }).ToList(),

            // Documents
            Documents = order.Documents.Select(d => new OrderDocumentDto
            {
                Id = d.Id,
                FileName = d.FileName,
                FilePath = d.FilePath,
                FileSize = d.FileSize,
                FileType = d.FileType,
                UploadedBy = d.UploadedBy,
                UploadedAt = d.UploadedAt,
                Description = d.Description
            }).ToList(),

            // Comments
            Comments = order.Comments.Select(c => new OrderCommentDto
            {
                Id = c.Id,
                UserId = c.UserId,
                Comment = c.Comment,
                IsInternal = c.IsInternal,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            }).ToList(),

            // Workflow History
            WorkflowHistory = order.WorkflowHistories.Select(w => new WorkflowHistoryDto
            {
                FromStatusNameKa = w.FromStatus?.NameKa,
                ToStatusNameKa = w.ToStatus?.NameKa ?? "უცნობი",
                ChangedByName = "სისტემა", // TODO: join AppUsers თუ გჭირდება
                ChangedAt = w.ChangedAt,
                Comments = w.Comments
            }).ToList()
        };

        return Ok(dto);
    }

    // POST: api/orders
    [HttpPost]
    public async Task<ActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        try
        {
            _logger.LogInformation("Creating order: {@Order}", dto);

            // OrderType შემოწმება
            var orderType = await _context.Set<OrderType>()
                .FirstOrDefaultAsync(ot => ot.Id == dto.OrderTypeId);

            if (orderType == null)
                return BadRequest(new { message = "არასწორი შეკვეთის ტიპი" });

            // Department შემოწმება
            if (dto.DepartmentId.HasValue)
            {
                var deptExists = await _context.Departments
                    .AnyAsync(d => d.Id == dto.DepartmentId.Value);

                if (!deptExists)
                    return BadRequest(new { message = "არასწორი დეპარტამენტი" });
            }

            // მომხმარებლის ID JWT-დან
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int requesterId))
                return Unauthorized(new { message = "მომხმარებელი ვერ დადგინდა" });

            // Default Status - "pending"
            var pendingStatus = await _context.Set<OrderStatus>()
                .FirstOrDefaultAsync(s => s.Code == "pending");

            if (pendingStatus == null)
                return BadRequest(new { message = "სტატუსი 'pending' არ არსებობს. შექმენით პირველ რიგში." });

            // შეკვეთის ნომრის გენერაცია
            var orderNumber = await GenerateOrderNumber();

            var newOrder = new Order
            {
                OrderNumber = orderNumber,
                OrderTypeId = dto.OrderTypeId,
                StatusId = pendingStatus.Id,
                RequesterId = requesterId,
                DepartmentId = dto.DepartmentId,
                Title = dto.Title,
                Description = dto.Description,
                Priority = dto.Priority,
                EstimatedAmount = dto.EstimatedAmount,
                Currency = dto.Currency,
                RequestedDate = dto.RequestedDate,
                RequiredByDate = dto.RequiredByDate,
                CreatedBy = requesterId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Items-ის დამატება
            foreach (var itemDto in dto.Items)
            {
                var orderItem = new OrderItem
                {
                    ItemName = itemDto.ItemName,
                    Quantity = itemDto.Quantity,
                    UnitPrice = itemDto.UnitPrice,
                    TotalPrice = itemDto.UnitPrice.HasValue
                        ? itemDto.UnitPrice.Value * itemDto.Quantity
                        : null,
                    CreatedAt = DateTime.UtcNow
                };
                newOrder.Items.Add(orderItem);
            }

            _context.Orders.Add(newOrder);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Order created successfully: {OrderNumber}", orderNumber);

            return CreatedAtAction(
                nameof(GetOrder),
                new { id = newOrder.Id },
                new { id = newOrder.Id, orderNumber = newOrder.OrderNumber }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return BadRequest(new { message = "შეცდომა შეკვეთის შექმნისას: " + ex.Message });
        }
    }

    // GET: api/orders/order-types
    [HttpGet("order-types")]
    public async Task<ActionResult> GetOrderTypes()
    {
        var types = await _context.Set<OrderType>()
            .Where(ot => ot.IsActive)
            .OrderBy(ot => ot.Name)
            .ToListAsync();

        return Ok(types);
    }

    // GET: api/orders/order-statuses
    [HttpGet("order-statuses")]
    public async Task<ActionResult> GetOrderStatuses()
    {
        var statuses = await _context.Set<OrderStatus>()
            .Where(s => s.IsActive)
            .OrderBy(s => s.OrderSeq)
            .ToListAsync();

        return Ok(statuses);
    }

    [HttpPost("{id}/{status}")]
    public async Task<ActionResult<OrderDto>> ChangeStatus(int id, string status, [FromBody] ChangeStatusDto? dto = null)
    {
        try
        {
            var updated = await _orderService.ChangeStatusAsync(id, status, dto?.Comments);
            return Ok(updated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing order status");
            return BadRequest(new { message = "სტატუსის შეცვლა ვერ მოხერხდა: " + ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<OrderDto>> UpdateOrder(int id, [FromBody] UpdateOrderDto dto)
    {
        var order = await _orderService.UpdateOrderAsync(id, dto);
        return Ok(order);
    }

    // GET: api/orders/items/search?q=...
    [HttpGet("items/search")]
    public async Task<ActionResult> SearchOrderItems([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Ok(new List<object>());

        var items = await _context.Set<OrderItem>()
            .Include(oi => oi.Order)
            .Where(oi => oi.ItemName != null && oi.ItemName.Contains(q))
            .OrderByDescending(oi => oi.CreatedAt)
            .Take(20)
            .Select(oi => new
            {
                oi.Id,
                oi.OrderId,
                oi.ItemName,
                oi.Quantity,
                oi.UnitPrice,
                oi.TotalPrice,
                OrderNumber = oi.Order != null ? oi.Order.OrderNumber : null,
                OrderTitle = oi.Order != null ? oi.Order.Title : null
            })
            .ToListAsync();

        return Ok(items);
    }

    // POST: api/orders/{id}/documents
    [HttpPost("{id}/documents")]
    public async Task<ActionResult<OrderDocumentDto>> UploadDocument(int id, IFormFile file, [FromForm] string? description)
    {
        try
        {
            var order = await _context.Orders.FindAsync(id) ?? throw new NotFoundException("შეკვეთა არ მოიძებნა");

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "ფაილი არ არის მითითებული" });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new { message = "მომხმარებელი ვერ დადგინდა" });

            var uploadDir = Path.Combine("wwwroot", "uploads", "orders", id.ToString());
            Directory.CreateDirectory(uploadDir);

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadDir, fileName);
            var relativePath = $"/uploads/orders/{id}/{fileName}";

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var document = new OrderDocument
            {
                OrderId = id,
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

            var dto = new OrderDocumentDto
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

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document");
            return BadRequest(new { message = "ფაილის ატვირთვა ვერ მოხერხდა: " + ex.Message });
        }
    }

    // DELETE: api/orders/{id}/documents/{docId}
    [HttpDelete("{id}/documents/{docId}")]
    public async Task<IActionResult> DeleteDocument(int id, int docId)
    {
        var document = await _context.OrderDocuments
            .FirstOrDefaultAsync(d => d.OrderId == id && d.Id == docId);

        if (document == null)
            return NotFound(new { message = "დოკუმენტი არ მოიძებნა" });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized(new { message = "მომხმარებელი ვერ დადგინდა" });

        if (document.UploadedBy != userId)
            return NotFound("დოკუმენტის წაშლა არ შეიძლება: უფლება არ გაქვთ");

        var fullPath = Path.Combine("wwwroot", document.FilePath.TrimStart('/'));
        if (System.IO.File.Exists(fullPath))
            System.IO.File.Delete(fullPath);

        _context.OrderDocuments.Remove(document);
        await _context.SaveChangesAsync();

        return NoContent();
    }
    [HttpPut("{id}/documents/{docId}")]
    public async Task<ActionResult<OrderDocumentDto>> UpdateDocument(int id, int docId, [FromBody] UpdateDocumentDto dto)
    {
        var updated = await _orderService.UpdateDocumentAsync(id, docId, dto);
        return Ok(updated);
    }




    // POST: api/orders/{id}/comments
    [HttpPost("{id}/comments")]
    public async Task<IActionResult> AddComment(int id, [FromBody] AddCommentDto dto)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null)
            return NotFound(new { message = "შეკვეთა არ მოიძებნა" });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized(new { message = "მომხმარებელი ვერ დადგინდა" });

        var comment = new OrderComment
        {
            OrderId = id,
            UserId = userId,
            Comment = dto.Comment,
            IsInternal = dto.IsInternal,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.OrderComments.Add(comment);
        await _context.SaveChangesAsync();

        return Ok();
    }

    public class AddCommentDto
    {
        public string Comment { get; set; } = string.Empty;
        public bool IsInternal { get; set; }
    }

    // Helper: შეკვეთის ნომრის გენერაცია (შენი ორიგინალური)
    private async Task<string> GenerateOrderNumber()
    {
        var prefix = "ORD";
        var year = DateTime.UtcNow.Year;
        var month = DateTime.UtcNow.Month;

        var lastOrder = await _context.Orders
            .Where(o => o.OrderNumber.StartsWith($"{prefix}-{year}{month:D2}"))
            .OrderByDescending(o => o.OrderNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastOrder != null)
        {
            var parts = lastOrder.OrderNumber.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1].Substring(6), out int lastNum))
            {
                nextNumber = lastNum + 1;
            }
        }

        return $"{prefix}-{year}{month:D2}{nextNumber:D4}";
    }
}