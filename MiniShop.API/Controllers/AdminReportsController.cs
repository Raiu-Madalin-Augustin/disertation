using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShop.API.Data;
using MiniShop.API.dto;
using MiniShop.API.Filters;

namespace MiniShop.API.Controllers;

[ApiController]
[Route("api/admin/reports")]
[ServiceFilter(typeof(AdminOnlyAttribute))]
public class AdminReportsController : ControllerBase
{
    private readonly MiniShopContext _ctx;
    public AdminReportsController(MiniShopContext ctx) => _ctx = ctx;

    // GET: api/admin/reports/low-stock?threshold=5&categoryId=1
    [HttpGet("low-stock")]
    public async Task<IActionResult> LowStock([FromQuery] int threshold = 5, [FromQuery] int? categoryId = null)
    {
        if (threshold < 0) return BadRequest("threshold must be >= 0");

        var q = _ctx.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.Stock <= threshold);

        if (categoryId is > 0)
            q = q.Where(p => p.CategoryId == categoryId);

        var data = await q
            .OrderBy(p => p.Stock).ThenBy(p => p.Name)
            .Select(p => new LowStockItemDto(
                p.Id, p.Name, p.Stock, p.Price, p.CategoryId, p.Category.Name
            ))
            .ToListAsync();

        return Ok(new
        {
            threshold,
            categoryId,
            count = data.Count,
            items = data
        });
    }

    // GET: api/admin/reports/sales-by-category?from=2025-07-01&to=2025-08-31
    [HttpGet("sales-by-category")]
    public async Task<IActionResult> SalesByCategory([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        NormalizeRange(ref from, ref to);

        var data = await _ctx.OrderItems
            .AsNoTracking()
            .Join(_ctx.Orders, oi => oi.OrderId, o => o.Id, (oi, o) => new { oi, o })
            .Join(_ctx.Products, x => x.oi.ProductId, p => p.Id, (x, p) => new { x.oi, x.o, p })
            .Join(_ctx.Categories, x => x.p.CategoryId, c => c.Id, (x, c) => new { x.oi, x.o, x.p, c })
            .Where(x => x.o.CreatedAt >= from && x.o.CreatedAt < to)
            .GroupBy(x => new { x.c.Id, x.c.Name })
            .Select(g => new SalesByCategoryRowDto(
                g.Key.Id,
                g.Key.Name,
                g.Sum(r => r.oi.Quantity),
                g.Sum(r => r.oi.Quantity * r.oi.Price)
            ))

            .OrderByDescending(r => r.TotalRevenue)
            .ToListAsync();

        return Ok(new
        {
            from,
            to,
            rows = data,
            totalRevenue = data.Sum(r => r.TotalRevenue),
            totalQty = data.Sum(r => r.TotalQty)
        });
    }

    // GET: api/admin/reports/top-products?from=2025-07-01&to=2025-08-31&top=10&sort=revenue
    // sort: "qty" | "revenue"
    [HttpGet("top-products")]
    public async Task<IActionResult> TopProducts([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null,
                                                 [FromQuery] int top = 10, [FromQuery] string sort = "revenue")
    {
        NormalizeRange(ref from, ref to);
        if (top <= 0) top = 10;

        var baseQuery = _ctx.OrderItems
            .AsNoTracking()
            .Join(_ctx.Orders, oi => oi.OrderId, o => o.Id, (oi, o) => new { oi, o })
            .Join(_ctx.Products, x => x.oi.ProductId, p => p.Id, (x, p) => new { x.oi, x.o, p })
            .Join(_ctx.Categories, x => x.p.CategoryId, c => c.Id, (x, c) => new { x.oi, x.o, x.p, c })
            .Where(x => x.o.CreatedAt >= from && x.o.CreatedAt < to)
            .GroupBy(x => new { x.p.Id, x.p.Name })
            .Select(g => new TopProductRowDto(
                 g.Key.Id,
                 g.Key.Name,
                 g.Key.Id,      
                g.Key.Name,    
                g.Sum(r => r.oi.Quantity),
                 g.Sum(r => r.oi.Quantity * r.oi.Price)
            ));

        // FIX: rebuild with proper keys (split keys)
        var baseQuery2 = _ctx.OrderItems
            .AsNoTracking()
            .Join(_ctx.Orders, oi => oi.OrderId, o => o.Id, (oi, o) => new { oi, o })
            .Join(_ctx.Products, x => x.oi.ProductId, p => p.Id, (x, p) => new { x.oi, x.o, p })
            .Join(_ctx.Categories, x => x.p.CategoryId, c => c.Id, (x, c) => new { x.oi, x.o, x.p, c })
            .Where(x => x.o.CreatedAt >= from && x.o.CreatedAt < to)
            .GroupBy(x => new { ProductId = x.p.Id, ProductName = x.p.Name, CategoryId = x.c.Id, CategoryName = x.c.Name })
            .Select(g => new TopProductRowDto(
                g.Key.ProductId,
                g.Key.ProductName,
                g.Key.CategoryId,
                g.Key.CategoryName,
                 g.Sum(r => r.oi.Quantity),
                 g.Sum(r => r.oi.Quantity * r.oi.Price)
            ));

        IQueryable<TopProductRowDto> ordered = sort.ToLower() switch
        {
            "qty" => baseQuery2.OrderByDescending(r => r.TotalQty).ThenBy(r => r.ProductName),
            _ => baseQuery2.OrderByDescending(r => r.TotalRevenue).ThenBy(r => r.ProductName),
        };

        var data = await ordered.Take(top).ToListAsync();

        return Ok(new
        {
            from,
            to,
            sort,
            top,
            rows = data
        });
    }

    // GET: api/admin/reports/inventory-valuation
    [HttpGet("inventory-valuation")]
    public async Task<IActionResult> InventoryValuation()
    {
        var rows = await _ctx.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .GroupBy(p => new { p.CategoryId, p.Category.Name })
            .Select(g => new InventoryValuationRowDto(
                g.Key.CategoryId,
                g.Key.Name,
                 g.Count(),
                g.Sum(p => p.Stock),
                 g.Sum(p => p.Stock * p.Price)
            ))
            .OrderByDescending(r => r.Valuation)
            .ToListAsync();

        return Ok(new
        {
            totalValuation = rows.Sum(r => r.Valuation),
            totalQty = rows.Sum(r => r.TotalQty),
            categories = rows
        });
    }

    // GET: api/admin/reports/audit?from=2025-07-01&to=2025-08-31&action=CREATE_&take=100
    [HttpGet("audit")]
    public async Task<IActionResult> Audit([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null,
                                           [FromQuery] string? action = null, [FromQuery] int take = 100)
    {
        NormalizeRange(ref from, ref to);
        if (take <= 0) take = 100;

        var q = _ctx.AuditLogs
            .AsNoTracking()
            .Where(a => a.Timestamp >= from && a.Timestamp < to);

        if (!string.IsNullOrWhiteSpace(action))
        {
            var t = action.Trim().ToUpperInvariant();
            q = q.Where(a => a.Action.ToUpper().Contains(t));
        }

        var items = await q
            .OrderByDescending(a => a.Timestamp)
            .Take(take)
            .Select(a => new AuditLogRowDto(a.Id, a.PerformedBy, a.Action, a.Timestamp))
            .ToListAsync();

        return Ok(new
        {
            from,
            to,
            action,
            take,
            count = items.Count,
            items
        });
    }

    // helpers
    private static void NormalizeRange(ref DateTime? from, ref DateTime? to)
    {
        // implicit: ultimele 30 zile
        var utcNow = DateTime.UtcNow;
        if (to is null) to = utcNow;
        if (from is null) from = to.Value.AddDays(-30);

        // safety: from < to, și "to" exclusiv (interval [from, to))
        if (from >= to) from = to.Value.AddDays(-1);
        to = DateTime.SpecifyKind(to.Value, DateTimeKind.Utc);
        from = DateTime.SpecifyKind(from.Value, DateTimeKind.Utc);
    }
}
