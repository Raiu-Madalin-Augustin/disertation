using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShop.API.Data;
using MiniShop.API.dto;
using MiniShop.API.Models;

namespace MiniShop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly MiniShopContext _ctx;
        public CartController(MiniShopContext ctx) => _ctx = ctx;

        // GET api/cart/{userId}
        [HttpGet("{userId:int}")]
        public async Task<ActionResult<IEnumerable<CartItemDto>>> GetCart(int userId)
        {
            var items = await _ctx.CartItems
                .AsNoTracking()
                .Include(ci => ci.Product)
                .Where(ci => ci.UserId == userId)
                .Select(ci => new CartItemDto
                {
                    Id = ci.Id,
                    ProductId = ci.ProductId,
                    ProductName = ci.Product!.Name,
                    Price = ci.Product.Price,
                    Quantity = ci.Quantity,
                    Stock = ci.Product.Stock
                })
                .ToListAsync();

            return Ok(items);
        }

        // POST api/cart/add
        [HttpPost("add")]
        public async Task<IActionResult> Add(AddToCartRequest req)
        {
            var user = await _ctx.Users.FindAsync(req.UserId);
            if (user is null) return NotFound("User not found.");

            var product = await _ctx.Products.FindAsync(req.ProductId);
            if (product is null) return NotFound("Product not found.");

            if (req.Quantity <= 0) return BadRequest("Quantity must be > 0.");

            var existing = await _ctx.CartItems
                .FirstOrDefaultAsync(ci => ci.UserId == req.UserId && ci.ProductId == req.ProductId);

            var newQty = (existing?.Quantity ?? 0) + req.Quantity;

            if (newQty > product.Stock)
                return Conflict($"Insufficient stock. Available: {product.Stock}");

            if (existing is null)
            {
                _ctx.CartItems.Add(new CartItem
                {
                    UserId = req.UserId,
                    ProductId = req.ProductId,
                    Quantity = req.Quantity
                });
            }
            else
            {
                existing.Quantity = newQty;
            }

            await _ctx.SaveChangesAsync();
            return Ok("Added to cart.");
        }

        // PUT api/cart/{cartItemId}
        [HttpPut("{cartItemId:int}")]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, UpdateCartRequest req)
        {
            var item = await _ctx.CartItems
                .Include(ci => ci.Product)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId);
            if (item is null) return NotFound();

            if (req.Quantity <= 0) return BadRequest("Quantity must be > 0.");
            if (req.Quantity > item.Product!.Stock)
                return Conflict($"Insufficient stock. Available: {item.Product.Stock}");

            item.Quantity = req.Quantity;
            await _ctx.SaveChangesAsync();
            return Ok("Quantity updated.");
        }

        // DELETE api/cart/{cartItemId}
        [HttpDelete("{cartItemId:int}")]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            var item = await _ctx.CartItems.FindAsync(cartItemId);
            if (item is null) return NotFound();

            _ctx.CartItems.Remove(item);
            await _ctx.SaveChangesAsync();
            return Ok("Removed.");
        }

        // DELETE api/cart/clear/{userId}
        [HttpDelete("clear/{userId:int}")]
        public async Task<IActionResult> Clear(int userId)
        {
            var items = _ctx.CartItems.Where(ci => ci.UserId == userId);
            _ctx.CartItems.RemoveRange(items);
            await _ctx.SaveChangesAsync();
            return Ok("Cart cleared.");
        }
    }
}
