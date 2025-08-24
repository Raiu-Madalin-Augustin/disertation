using Testcontainers.MsSql;
using Microsoft.EntityFrameworkCore;
using MiniShop.API.Data;
using MiniShop.API.Models;

namespace MiniShop.Tests;

public class ProductsSearchTests : IAsyncLifetime
{
    private readonly MsSqlContainer _db = new MsSqlBuilder().Build();
    private MiniShopContext _ctx = default!;

    public async Task InitializeAsync()
    {
        await _db.StartAsync();
        var options = new DbContextOptionsBuilder<MiniShopContext>()
            .UseSqlServer(_db.GetConnectionString())
            .Options;

        _ctx = new MiniShopContext(options);
        await _ctx.Database.MigrateAsync();

        var cat = new Category { Name = "Electronice" };
        _ctx.Categories.Add(cat);
        _ctx.Products.AddRange(
            new Product { Name = "Casti Pro", Price = 299, Stock = 10, Category = cat },
            new Product { Name = "Mouse Basic", Price = 49, Stock = 50, Category = cat }
        );
        await _ctx.SaveChangesAsync();
    }

    public async Task DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task Search_By_Keyword_Returns_Expected()
    {
        var q = "casti".ToLower();
        var res = await _ctx.Products.Where(p => p.Name.ToLower().Contains(q)).ToListAsync();
        Assert.Single(res);
        Assert.Equal("Casti Pro", res[0].Name);
    }

    [Fact]
    public async Task Low_Stock_Filters_Correctly()
    {
        var low = await _ctx.Products.Where(p => p.Stock < 20).ToListAsync();
        Assert.Single(low);
        Assert.Equal("Casti Pro", low[0].Name);
    }
}
