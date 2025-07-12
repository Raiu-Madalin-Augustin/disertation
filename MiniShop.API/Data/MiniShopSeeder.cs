// MiniShop.API/Data/MiniShopSeeder.cs
using MiniShop.API.Models;
using Microsoft.EntityFrameworkCore;

namespace MiniShop.API.Data;

public static class MiniShopSeeder
{
    public static async Task SeedAsync(MiniShopContext ctx)
    {
        await ctx.Database.MigrateAsync();

        if (await ctx.Products.AnyAsync()) return;

        var categories = new[]
        {
            new Category { Name = "Electronice"   },
            new Category { Name = "Cărţi"         },
            new Category { Name = "Îmbrăcăminte"  },
            new Category { Name = "Casă & Bucătărie" }
        };
        ctx.Categories.AddRange(categories);
        await ctx.SaveChangesAsync();

        int cat(string name) => categories.First(c => c.Name == name).Id;

        var products = new List<Product?>
        {
            new Product { Name = "Căşti wireless", Description = "test",   Stock = 25, Price = 199.99m, CategoryId = cat("Electronice") },
            new Product { Name = "Mouse gaming",    Description = "test",  Stock = 40, Price = 149.99m, CategoryId = cat("Electronice") },
            new Product { Name = "Tastatură mecanică",Description = "test",Stock = 15, Price = 349.99m, CategoryId = cat("Electronice") },
            new Product { Name = "Fierbător apă",   Description = "test",  Stock = 18, Price = 89.90m,  CategoryId = cat("Casă & Bucătărie") },
            new Product { Name = "Set cuțite inox",   Description = "test",Stock = 12, Price = 129.00m, CategoryId = cat("Casă & Bucătărie") },
        };
        ctx.Products.AddRange(products!);

        var users = new[]
        {
            new User { Username = "alice@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pa$$w0rd"), Role = Role.Client },
            new User { Username = "admin@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"), Role = Role.Admin  }
        };
        ctx.Users.AddRange(users);

        await ctx.SaveChangesAsync();
    }
}
