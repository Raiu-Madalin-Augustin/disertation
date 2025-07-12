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

        var category = new Category { Name = "Test" };
        var product = new Product
        {
            Name = "Căști test",
            Price = 99.99m,
            Stock = 10,
            Category = category
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task Product_Should_Be_Inserted_Into_Database()
    {
        var product = await _context.Products.FirstOrDefaultAsync();
        Assert.NotNull(product);
        Assert.Equal("Căști test", product!.Name);
    }
    

    [Fact]
    public async Task Add_Product_Should_Increase_Product_Count()
    {
        int initialCount = await _context.Products.CountAsync();

        var category = new Category { Name = "Nouă" };
        var product = new Product
        {
            Name = "Tastatură mecanică",
            Price = 250.00m,
            Stock = 5,
            Category = category
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        int finalCount = await _context.Products.CountAsync();
        Assert.Equal(initialCount + 1, finalCount);
    }

    [Fact]
    public async Task PlacingOrder_Should_Decrease_Product_Stock()
    {
        var category = new Category { Name = "Periferice" };
        var product = new Product
        {
            Name = "Mouse optic",
            Price = 99.00m,
            Stock = 10,
            Category = category
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var user = new User { Username = "testuser", PasswordHash = "hash" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var order = new Order
        {
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            Items =
        [
            new() {
                ProductId = product.Id,
                Quantity = 2,
                Price = product.Price
            },

        ]
        };

        _context.Orders.Add(order);
        product.Stock -= 2;

        await _context.SaveChangesAsync();

        var updatedProduct = await _context.Products.FindAsync(product.Id);
        Assert.Equal(8, updatedProduct!.Stock);
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }
}
