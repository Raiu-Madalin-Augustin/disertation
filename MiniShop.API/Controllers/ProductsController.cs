using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShop.API.Data;
using MiniShop.API.dto;
using MiniShop.API.Models;
using System.Linq;

namespace MiniShop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly MiniShopContext _context;

        public ProductsController(MiniShopContext context)
        {
            _context = context;
        }

        // GET: api/products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return await _context.Products.Include(p => p.Category).ToListAsync();
        }

        // POST: api/products
        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProducts), new { id = product.Id }, product);
        }

        [HttpGet("search")]
        public async Task<ActionResult<PagedResult<ProductDto>>> Search([FromQuery] ProductQuery q)
        {
            if (q.Page <= 0) q.Page = 1;
            if (q.PageSize <= 0 || q.PageSize > 100) q.PageSize = 10;

            IQueryable<Product> query = _context.Products
                .AsNoTracking()
                .Include(p => p.Category);

            // Filtrări
            if (q.CategoryId.HasValue)
                query = query.Where(p => p.CategoryId == q.CategoryId.Value);

            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                var s = q.Search.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(s)
                                       || (p.Category != null && p.Category.Name.ToLower().Contains(s)));
            }

            if (q.MinPrice.HasValue)
                query = query.Where(p => p.Price >= q.MinPrice.Value);

            if (q.MaxPrice.HasValue)
                query = query.Where(p => p.Price <= q.MaxPrice.Value);

            // Sortare
            bool desc = (q.SortDir ?? "asc").Equals("desc", StringComparison.OrdinalIgnoreCase);
            query = (q.SortBy ?? "name").ToLower() switch
            {
                "price" => desc ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
                "stock" => desc ? query.OrderByDescending(p => p.Stock) : query.OrderBy(p => p.Stock),
                _ => desc ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name)
            };

            // Total + paginare
            var total = await query.CountAsync();
            var items = await query
                .Skip((q.Page - 1) * q.PageSize)
                .Take(q.PageSize)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Stock = p.Stock,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : ""
                })
                .ToListAsync();

            return Ok(new PagedResult<ProductDto>
            {
                Total = total,
                Page = q.Page,
                PageSize = q.PageSize,
                Items = items
            });
        }

        /// <summary>
        /// (Opțional) Rapoarte de stocuri scăzute – util pentru F7.
        /// </summary>
        [HttpGet("low-stock")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> LowStock([FromQuery] int threshold = 5)
        {
            var items = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.Stock < threshold)
                .OrderBy(p => p.Stock)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Stock = p.Stock,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : ""
                })
                .ToListAsync();

            return Ok(items);
        }
    }
}
