using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ViettalAPI.Data;
using ViettalAPI.Models;

namespace ViettalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly ViettalDbContext _context;

        public OrdersController(ViettalDbContext context)
        {
            _context = context;
        }

        // GET: api/orders
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<SimOrder>>> GetOrders()
        {
            return await _context.Orders.OrderByDescending(o => o.CreatedAt).ToListAsync();
        }

        // GET: api/orders/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<SimOrder>>> GetUserOrders(string userId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole(UserRole.Admin.ToString());

            if (currentUserId != userId && !isAdmin)
            {
                return Forbid();
            }

            return await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        // POST: api/orders
        [HttpPost]
        public async Task<ActionResult<SimOrder>> PostOrder([FromBody] SimOrder order)
        {
            if (order == null)
            {
                return BadRequest(new { message = "Dữ liệu đơn hàng không hợp lệ." });
            }

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            // Force the Order's UserId to match the authenticated user
            order.UserId = currentUserId;

            // Generate order ID if not provided
            if (string.IsNullOrWhiteSpace(order.Id))
            {
                order.Id = "ORD-" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var sim = await _context.Sims.FindAsync(order.SimId);
                    if (sim == null)
                    {
                        return BadRequest(new { message = "Không tìm thấy SIM cần đặt mua." });
                    }

                    if (sim.Status != SimStatus.Available)
                    {
                        return BadRequest(new { message = "SIM này vừa được đặt mua. Vui lòng chọn SIM khác." });
                    }

                    // Force values matching database state
                    order.TotalPrice = sim.Price;
                    order.Status = OrderStatus.Pending;
                    order.CreatedAt = DateTime.UtcNow;

                    // Update Sim status to Sold
                    sim.Status = SimStatus.Sold;

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return CreatedAtAction(nameof(GetUserOrders), new { userId = order.UserId }, order);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { message = "Đã xảy ra lỗi hệ thống: " + ex.Message });
                }
            }
        }

        // PUT: api/orders/{id}/status
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrderStatus(string id, [FromBody] OrderStatusUpdateDto dto)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound(new { message = $"Không tìm thấy đơn hàng với ID: {id}" });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var oldStatus = order.Status;
                    order.Status = dto.Status;

                    var sim = await _context.Sims.FindAsync(order.SimId);
                    if (sim != null)
                    {
                        if (dto.Status == OrderStatus.Completed || dto.Status == OrderStatus.Confirmed)
                        {
                            sim.Status = SimStatus.Sold;
                        }
                        else if (dto.Status == OrderStatus.Cancelled || dto.Status == OrderStatus.Pending)
                        {
                            // Verify if there are no other active confirmed/completed orders for this SIM before restoring to available
                            var otherSoldActiveOrdersExist = await _context.Orders
                                .AnyAsync(o => o.SimId == order.SimId && o.Id != id && 
                                               (o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Completed));
                            if (!otherSoldActiveOrdersExist)
                            {
                                sim.Status = SimStatus.Available;
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return NoContent();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { message = "Lỗi khi cập nhật trạng thái đơn hàng: " + ex.Message });
                }
            }
        }

        // POST: api/orders/{id}/cancel
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(string id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound(new { message = $"Không tìm thấy đơn hàng với ID: {id}" });
            }

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole(UserRole.Admin.ToString());

            if (order.UserId != currentUserId && !isAdmin)
            {
                return Forbid();
            }

            if (order.Status != OrderStatus.Pending)
            {
                return BadRequest(new { message = "Chỉ có thể hủy các đơn hàng đang ở trạng thái chờ xử lý." });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    order.Status = OrderStatus.Cancelled;

                    var sim = await _context.Sims.FindAsync(order.SimId);
                    if (sim != null)
                    {
                        // Verify if there are no other active confirmed/completed orders for this SIM
                        var otherSoldActiveOrdersExist = await _context.Orders
                            .AnyAsync(o => o.SimId == order.SimId && o.Id != id && 
                                           (o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Completed));
                        if (!otherSoldActiveOrdersExist)
                        {
                            sim.Status = SimStatus.Available;
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return NoContent();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { message = "Lỗi khi hủy đơn hàng: " + ex.Message });
                }
            }
        }
    }

    public class OrderStatusUpdateDto
    {
        public OrderStatus Status { get; set; }
    }
}
