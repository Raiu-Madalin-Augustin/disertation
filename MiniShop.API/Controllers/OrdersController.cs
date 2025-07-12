using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShop.API.Data;
using MiniShop.API.Models;

namespace MiniShop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly MiniShopContext _context;

        public OrdersController(MiniShopContext context)
        {
            _context = context;
        }

        // GET: api/orders/user/1
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrdersForUser(int userId)
        {
            var orders = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId)
                .ToListAsync();

            if (!orders.Any())
                return NotFound("Utilizatorul nu are comenzi.");

            return Ok(orders);
        }

        // POST: api/orders
        [HttpPost]
        public async Task<ActionResult<Order>> PlaceOrder(Order order)
        {
            order.CreatedAt = DateTime.UtcNow;
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOrdersForUser), new { userId = order.UserId }, order);
        }
    }
}
