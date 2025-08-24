using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShop.API.Data;
using MiniShop.API.dto;
using MiniShop.API.Filters;
using MiniShop.API.Models;

namespace MiniShop.API.Controllers;

[ApiController]
[Route("api/admin/categories")]
[ServiceFilter(typeof(AdminOnlyAttribute))]
public class AdminCategoriesController : ControllerBase
{
    private readonly MiniShopContext _ctx;
    public AdminCategoriesController(MiniShopContext ctx) => _ctx = ctx;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? q, [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var query = _ctx.Categories.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var t = q.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(t));
        }

        var items = await query
            .OrderBy(c => c.Name)
            .Skip(Math.Max(0, skip))
            .Take(take <= 0 ? 50 : take)
            .Select(c => new
            {
                c.Id,
                c.Name,
                ProductsCount = c.Products.Count
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var cat = await _ctx.Categories
            .AsNoTracking()
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cat is null) return NotFound("Category not found.");

        return Ok(new
        {
            cat.Id,
            cat.Name,
            Products = cat.Products.Select(p => new { p.Id, p.Name }).ToList()
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
    {
        var name = (dto.Name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Name is required.");

        var exists = await _ctx.Categories.AnyAsync(c => c.Name.ToLower() == name.ToLower());
        if (exists) return Conflict("Category with this name already exists.");

        var cat = new Category { Name = name };
        _ctx.Categories.Add(cat);
        await _ctx.SaveChangesAsync();
        await AuditAsync($"CREATE_CATEGORY id={cat.Id} name='{cat.Name}'");

        return CreatedAtAction(nameof(GetById), new { id = cat.Id }, new { cat.Id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryDto dto)
    {
        var cat = await _ctx.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (cat is null) return NotFound("Category not found.");

        var name = (dto.Name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Name is required.");

        var duplicate = await _ctx.Categories.AnyAsync(c => c.Id != id && c.Name.ToLower() == name.ToLower());
        if (duplicate) return Conflict("Another category with this name already exists.");

        cat.Name = name;
        await _ctx.SaveChangesAsync();
        await AuditAsync($"UPDATE_CATEGORY id={cat.Id} name='{cat.Name}'");

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var cat = await _ctx.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
        if (cat is null) return NotFound("Category not found.");
        if (cat.Products.Any())
            return Conflict("Category has products and cannot be deleted.");

        _ctx.Categories.Remove(cat);
        await _ctx.SaveChangesAsync();
        await AuditAsync($"DELETE_CATEGORY id={id}");

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
