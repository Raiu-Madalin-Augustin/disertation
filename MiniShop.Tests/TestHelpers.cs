using Microsoft.EntityFrameworkCore;
using MiniShop.API.Data;
using MiniShop.API.Models;

namespace MiniShop.Tests;

public static class TestHelpers
{
    public static MiniShopContext CreateContext(string dbName = null!)
    {
        var options = new DbContextOptionsBuilder<MiniShopContext>()
            .UseInMemoryDatabase(databaseName: dbName ?? Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        return new MiniShopContext(options);
    }

    public static async Task SeedBasicAsync(MiniShopContext ctx)
    {
        if (await ctx.Users.AnyAsync()) return;

        var cat1 = new Category { Name = "Electronice" };
        var cat2 = new Category { Name = "Carti" };
        ctx.Categories.AddRange(cat1, cat2);
        await ctx.SaveChangesAsync();

        ctx.Products.AddRange(
            new Product { Name = "Casti", Description = "ok", Stock = 10, Price = 100m, CategoryId = cat1.Id },
            new Product { Name = "Mouse", Description = "ok", Stock = 5, Price = 75m, CategoryId = cat1.Id },
            new Product { Name = "Roman", Description = "ok", Stock = 20, Price = 50m, CategoryId = cat2.Id }
        );

        var admin = new User
        {
            Username = "admin",
            Email = "admin@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            IsAdmin = true,
            Role = Role.Admin
        };
        var client = new User
        {
            Username = "alice",
            Email = "alice@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pa$$w0rd"),
            IsAdmin = false,
            Role = Role.Client
        };

        ctx.Users.AddRange(admin, client);
        await ctx.SaveChangesAsync();
    }

    public static async Task<int> AddCartItem(MiniShopContext ctx, int userId, int productId, int qty)
    {
        var ci = new CartItem { UserId = userId, ProductId = productId, Quantity = qty };
        ctx.CartItems.Add(ci);
        await ctx.SaveChangesAsync();
        return ci.Id;
    }
}

