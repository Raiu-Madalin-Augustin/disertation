using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShop.API.Data;
using MiniShop.API.Models;
using MiniShop.API.dto;
using MiniShop.API.Filters;

namespace MiniShop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly MiniShopContext _ctx;

        public CategoriesController(MiniShopContext ctx)
        {
            _ctx = ctx;
        }

        /// <summary>Listă categorii (cu numărul de produse).</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _ctx.Categories
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    ProductsCount = _ctx.Products.Count(p => p.CategoryId == c.Id)
                })
                .OrderBy(c => c.Name)
                .ToListAsync();

            return Ok(data);
        }

        /// <summary>Creare categorie nouă.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CategoryCreateRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest("Name is required.");

            var exists = await _ctx.Categories.AnyAsync(c => c.Name == req.Name);
            if (exists) return Conflict("A category with the same name already exists.");

            var cat = new Category { Name = req.Name };
            _ctx.Categories.Add(cat);

            _ctx.AuditLogs.Add(new AuditLog
            {
                PerformedBy = User.Identity?.Name ?? "admin",
                Action = $"CREATE_CATEGORY '{req.Name}'",
                Timestamp = DateTime.UtcNow
            });

            await _ctx.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = cat.Id }, new { cat.Id, cat.Name });
        }

        /// <summary>Categorie după Id.</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var cat = await _ctx.Categories.FindAsync(id);
            if (cat == null) return NotFound();

            var dto = new
            {
                cat.Id,
                cat.Name,
                ProductsCount = await _ctx.Products.CountAsync(p => p.CategoryId == cat.Id)
            };
            return Ok(dto);
        }

        /// <summary>Actualizare categorie.</summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] CategoryUpdateRequest req)
        {
            if (id != req.Id) return BadRequest("Route id and body id must match.");
            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest("Name is required.");

            var cat = await _ctx.Categories.FindAsync(id);
            if (cat == null) return NotFound();

            var nameTaken = await _ctx.Categories
                .AnyAsync(c => c.Id != id && c.Name == req.Name);
            if (nameTaken) return Conflict("Another category with the same name already exists.");

            cat.Name = req.Name;
            _ctx.Categories.Update(cat);

            _ctx.AuditLogs.Add(new AuditLog
            {
                PerformedBy = User.Identity?.Name ?? "admin",
                Action = $"UPDATE_CATEGORY #{id} -> '{req.Name}'",
                Timestamp = DateTime.UtcNow
            });

            await _ctx.SaveChangesAsync();
            return Ok(new { cat.Id, cat.Name });
        }

        /// <summary>Ștergere categorie (doar dacă nu are produse).</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var cat = await _ctx.Categories.FindAsync(id);
            if (cat == null) return NotFound();

            var hasProducts = await _ctx.Products.AnyAsync(p => p.CategoryId == id);
            if (hasProducts) return Conflict("Cannot delete a category that still has products.");

            _ctx.Categories.Remove(cat);

            _ctx.AuditLogs.Add(new AuditLog
            {
                PerformedBy = User.Identity?.Name ?? "admin",
                Action = $"DELETE_CATEGORY #{id}",
                Timestamp = DateTime.UtcNow
            });

            await _ctx.SaveChangesAsync();
            return NoContent();
        }
    }
}
