using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShop.API.Data;
using MiniShop.API.dto;
using MiniShop.API.Models;

namespace MiniShop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly MiniShopContext _ctx;
        public OrdersController(MiniShopContext ctx) => _ctx = ctx;

        /// <summary>Returnează comenzile unui utilizator (cu items).</summary>
        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            var exists = await _ctx.Users.AnyAsync(u => u.Id == userId);
            if (!exists) return NotFound("User not found.");

            var orders = await _ctx.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    o.Id,
                    o.CreatedAt,
                    Items = _ctx.OrderItems
                        .Where(i => i.OrderId == o.Id)
                        .Select(i => new { i.ProductId, i.Quantity, i.Price })
                        .ToList()
                })
                .ToListAsync();

            return Ok(orders);
        }

        /// <summary>Plasează o comandă din coșul utilizatorului.</summary>
        [HttpPost("place/{userId:int}")]
        public async Task<IActionResult> Place(int userId)
        {
            // 1) verificări inițiale
            var user = await _ctx.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null) return NotFound("User not found.");

            var cart = await _ctx.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (cart.Count == 0) return BadRequest("Cart is empty.");

            // 2) tranzacție
            using var tx = await _ctx.Database.BeginTransactionAsync();
            try
            {
                // 2.1) validare stoc
                foreach (var item in cart)
                {
                    if (item.Product is null) return BadRequest($"Product {item.ProductId} not found.");
                    if (item.Quantity <= 0) return BadRequest("Invalid quantity.");
                    if (item.Product.Stock < item.Quantity)
                        return Conflict($"Insufficient stock for product {item.Product.Name}.");
                }

                // 2.2) creează Order
                var order = new Order
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    Items = new List<OrderItem>()
                };
                _ctx.Orders.Add(order);
                await _ctx.SaveChangesAsync(); // ca să avem Id

                decimal total = 0m;

                // 2.3) creează OrderItems + scade stocul
                foreach (var item in cart)
                {
                    var price = item.Product!.Price;
                    total += price * item.Quantity;

                    _ctx.OrderItems.Add(new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = price
                    });

                    item.Product.Stock -= item.Quantity;
                    _ctx.Products.Update(item.Product);
                }

                // 2.4) golește coșul
                _ctx.CartItems.RemoveRange(cart);

                // 2.5) audit log
                _ctx.AuditLogs.Add(new AuditLog
                {
                    PerformedBy = userId.ToString(),
                    Action = $"PLACE_ORDER #{order.Id} items={cart.Count}",
                    Timestamp = DateTime.UtcNow
                });

                await _ctx.SaveChangesAsync();
                await tx.CommitAsync();

                var resp = new PlaceOrderResponseDto
                {
                    OrderId = order.Id,
                    CreatedAt = order.CreatedAt,
                    ItemsCount = cart.Count,
                    Total = total
                };
                return Ok(resp);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, $"Failed to place order: {ex.Message}");
            }
        }
    }
}