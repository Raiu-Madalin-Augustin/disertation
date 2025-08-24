using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShop.API.Data;
using MiniShop.API.dto;
using MiniShop.API.Filters;
using MiniShop.API.Models;

namespace MiniShop.API.Controllers;

[ApiController]
[Route("api/admin/products")]
[ServiceFilter(typeof(AdminOnlyAttribute))]
public class AdminProductsController : ControllerBase
{
    private readonly MiniShopContext _ctx;
    public AdminProductsController(MiniShopContext ctx) => _ctx = ctx;

    // GET: api/admin/products?q=mouse&categoryId=1&skip=0&take=50
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? q, [FromQuery] int? categoryId, [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var query = _ctx.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var t = q.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(t) ||
                (p.Description != null && p.Description.ToLower().Contains(t)));
        }

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        var items = await query
            .OrderBy(p => p.Name)
            .Skip(Math.Max(0, skip))
            .Take(take <= 0 ? 50 : take)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Price,
                p.Stock,
                p.CategoryId,
                CategoryName = p.Category.Name
            })
            .ToListAsync();

        return Ok(items);
    }

    // GET: api/admin/products/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var p = await _ctx.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound("Product not found.");

        return Ok(new
        {
            p.Id,
            p.Name,
            p.Description,
            p.ImageUrl,
            p.Price,
            p.Stock,
            p.CategoryId,
            CategoryName = p.Category?.Name
        });
    }

    // POST: api/admin/products
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
    {
        var name = (dto.Name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name)) return BadRequest("Name is required.");
        if (dto.Price < 0) return BadRequest("Price cannot be negative.");
        if (dto.Stock < 0) return BadRequest("Stock cannot be negative.");

        var catExists = await _ctx.Categories.AnyAsync(c => c.Id == dto.CategoryId);
        if (!catExists) return BadRequest("Category does not exist.");

        // optional: same-name-in-category uniqueness
        var duplicate = await _ctx.Products.AnyAsync(p =>
            p.CategoryId == dto.CategoryId && p.Name.ToLower() == name.ToLower());
        if (duplicate) return Conflict("A product with the same name already exists in this category.");

        var p = new Product
        {
            Name = name,
            Description = dto.Description,
            ImageUrl = dto.ImageUrl,
            Price = dto.Price,
            Stock = dto.Stock,
            CategoryId = dto.CategoryId
        };

        _ctx.Products.Add(p);
        await _ctx.SaveChangesAsync();
        await AuditAsync($"CREATE_PRODUCT id={p.Id} name='{p.Name}' catId={p.CategoryId}");

        return CreatedAtAction(nameof(GetById), new { id = p.Id }, new { p.Id });
    }

    // PUT: api/admin/products/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto)
    {
        var p = await _ctx.Products.FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound("Product not found.");

        var name = (dto.Name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name)) return BadRequest("Name is required.");
        if (dto.Price < 0) return BadRequest("Price cannot be negative.");
        if (dto.Stock < 0) return BadRequest("Stock cannot be negative.");

        var catExists = await _ctx.Categories.AnyAsync(c => c.Id == dto.CategoryId);
        if (!catExists) return BadRequest("Category does not exist.");

        var duplicate = await _ctx.Products.AnyAsync(x =>
            x.Id != id &&
            x.CategoryId == dto.CategoryId &&
            x.Name.ToLower() == name.ToLower());
        if (duplicate) return Conflict("Another product with the same name exists in this category.");

        p.Name = name;
        p.Description = dto.Description;
        p.ImageUrl = dto.ImageUrl;
        p.Price = dto.Price;
        p.Stock = dto.Stock;
        p.CategoryId = dto.CategoryId;

        await _ctx.SaveChangesAsync();
        await AuditAsync($"UPDATE_PRODUCT id={p.Id} name='{p.Name}' catId={p.CategoryId}");

        return NoContent();
    }

    // DELETE: api/admin/products/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _ctx.Products.FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound("Product not found.");

        // optional: block delete if product is already in orders
        var inOrders = await _ctx.OrderItems.AnyAsync(oi => oi.ProductId == id);
        if (inOrders) return Conflict("Product is referenced in orders and cannot be deleted.");

        _ctx.Products.Remove(p);
        await _ctx.SaveChangesAsync();
        await AuditAsync($"DELETE_PRODUCT id={id}");

        return NoContent();
    }

    private async Task AuditAsync(string action)
    {
        var adminId = HttpContext.Request.Headers["X-User-Id"].FirstOrDefault() ?? "unknown";
        _ctx.AuditLogs.Add(new AuditLog
        {
            PerformedBy = adminId,
            Action = action,
            Timestamp = DateTime.UtcNow
        });
        await _ctx.SaveChangesAsync();
    }
}
