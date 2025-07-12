using Testcontainers.MsSql;
using Microsoft.EntityFrameworkCore;
using MiniShop.API.Data;
using MiniShop.API.Models;

namespace MiniShop.Tests;

public class CartTests : IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer;
    private MiniShopContext _context;

    public CartTests()
    {
        _dbContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("yourStrong(!)Password")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        var options = new DbContextOptionsBuilder<MiniShopContext>()
            .UseSqlServer(_dbContainer.GetConnectionString())
            .Options;

        _context = new MiniShopContext(options);
        await _context.Database.EnsureCreatedAsync();

        // Seed test product
        _context.Products.Add(new Product
        {
            Id = 1,
            Name = "Căști test",
            Price = 99.99m,
            Stock = 10,
            Category = new Category { Id = 1, Name = "Test" }
        });

        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task Product_Should_Be_Inserted_Into_Database()
    {
        var product = await _context.Products.FirstOrDefaultAsync();
        Assert.NotNull(product);
        Assert.Equal("Căști test", product!.Name);
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }
}
