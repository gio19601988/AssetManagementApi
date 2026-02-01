using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssetManagementApi.Data;
using AssetManagementApi.Dtos.Orders;
using AssetManagementApi.Models.Orders;
using AssetManagementApi.Models;
using System.Security.Claims;

namespace AssetManagementApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(ApplicationDbContext context, ILogger<OrdersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ✅ GET: api/orders
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
                RequesterName = "მომხმარებელი", // TODO: join with AppUsers
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

    // ✅ GET: api/orders/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDetailDto>> GetOrder(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Status)
            .Include(o => o.OrderType)
            .Include(o => o.Items)
            .Where(o => o.Id == id)
            .Select(o => new OrderDetailDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                
                // Type
                OrderTypeId = o.OrderTypeId,
                OrderTypeName = o.OrderType != null ? o.OrderType.Name : null,
                OrderTypeNameKa = o.OrderType != null ? o.OrderType.NameKa : null,
                
                // Status
                StatusId = o.StatusId,
                StatusCode = o.Status != null ? o.Status.Code : "unknown",
                StatusName = o.Status != null ? o.Status.Name : "Unknown",
                StatusNameKa = o.Status != null ? o.Status.NameKa : "უცნობი",
                
                // Basic
                RequesterId = o.RequesterId,
                RequesterName = "მომხმარებელი", // TODO: join with AppUsers
                DepartmentId = o.DepartmentId,
                DepartmentName = null, // TODO: join with Departments
                
                Title = o.Title,
                Description = o.Description,
                Priority = o.Priority,
                
                // Financial
                EstimatedAmount = o.EstimatedAmount,
                Currency = o.Currency,
                
                // Dates
                RequestedDate = o.RequestedDate,
                RequiredByDate = o.RequiredByDate,
                ApprovedDate = o.ApprovedDate,
                CompletedDate = o.CompletedDate,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt,
                
                // Items
                Items = o.Items.Select(i => new OrderItemDto
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
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (order == null)
            return NotFound(new { message = "შეკვეთა არ მოიძებნა" });

        return Ok(order);
    }

    // ✅ POST: api/orders
    [HttpPost]
    public async Task<ActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        try
        {
            _logger.LogInformation("Creating order: {@Order}", dto);

            // ვამოწმებთ OrderType-ს
            var orderType = await _context.Set<OrderType>()
                .FirstOrDefaultAsync(ot => ot.Id == dto.OrderTypeId);
            
            if (orderType == null)
                return BadRequest(new { message = "არასწორი შეკვეთის ტიპი" });

            // ვამოწმებთ Department-ს
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

    // ✅ GET: api/orders/order-types
    [HttpGet("order-types")]
    public async Task<ActionResult> GetOrderTypes()
    {
        var types = await _context.Set<OrderType>()
            .Where(ot => ot.IsActive)
            .OrderBy(ot => ot.Name)
            .ToListAsync();

        return Ok(types);
    }

    // ✅ GET: api/orders/order-statuses
    [HttpGet("order-statuses")]
    public async Task<ActionResult> GetOrderStatuses()
    {
        var statuses = await _context.Set<OrderStatus>()
            .Where(s => s.IsActive)
            .OrderBy(s => s.OrderSeq)
            .ToListAsync();

        return Ok(statuses);
    }

    // ✅ GET: api/orders/items/search?q=...
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

    // ✅ Helper: შეკვეთის ნომრის გენერაცია
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