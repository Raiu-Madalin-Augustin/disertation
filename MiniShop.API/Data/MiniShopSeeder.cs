// MiniShop.API/Data/MiniShopSeeder.cs
using MiniShop.API.Models;
using Microsoft.EntityFrameworkCore;

namespace MiniShop.API.Data;

public static class MiniShopSeeder
{
    public static async Task SeedAsync(MiniShopContext ctx)
    {
        await ctx.Database.MigrateAsync();

        // ---- CATEGORII + PRODUSE (idempotent) ----
        if (!await ctx.Categories.AnyAsync())
        {
            var cats = new[]
            {
                new Category { Name = "Electronice" },
                new Category { Name = "Cărţi" },
                new Category { Name = "Îmbrăcăminte" },
                new Category { Name = "Casă & Bucătărie" }
            };
            ctx.Categories.AddRange(cats);
            await ctx.SaveChangesAsync();

            int cat(string n) => cats.First(c => c.Name == n).Id;
            ctx.Products.AddRange(
                new Product { Name = "Căşti wireless", Description = "test", Stock = 25, Price = 199.99m, CategoryId = cat("Electronice") },
                new Product { Name = "Mouse gaming", Description = "test", Stock = 40, Price = 149.99m, CategoryId = cat("Electronice") },
                new Product { Name = "Tastatură mecanică", Description = "test", Stock = 15, Price = 349.99m, CategoryId = cat("Electronice") },
                new Product { Name = "Fierbător apă", Description = "test", Stock = 18, Price = 89.90m, CategoryId = cat("Casă & Bucătărie") },
                new Product { Name = "Set cuțite inox", Description = "test", Stock = 12, Price = 129.00m, CategoryId = cat("Casă & Bucătărie") }
            );
            await ctx.SaveChangesAsync();
        }

        // ---- UPSERT ADMIN ----
        const string adminEmail = "admin@example.com";
        var admin = await ctx.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
        if (admin is null)
        {
            admin = new User
            {
                Username = "admin",
                Email = adminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                IsAdmin = true,
                Role = Role.Admin
            };
            ctx.Users.Add(admin);
        }
        else
        {
            // ne asigurăm că are rol & parolă corectă
            admin.Username = string.IsNullOrEmpty(admin.Username) ? "admin" : admin.Username;
            admin.IsAdmin = true;
            admin.Role = Role.Admin;

            // dacă vrei să RESETEZI parola la fiecare pornire, decomentează:
            // admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!");
            ctx.Users.Update(admin);
        }

        // ---- UPSERT CLIENT DEMO ----
        const string clientEmail = "alice@example.com";
        var client = await ctx.Users.FirstOrDefaultAsync(u => u.Email == clientEmail);
        if (client is null)
        {
            client = new User
            {
                Username = "alice",
                Email = clientEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pa$$w0rd"),
                IsAdmin = false,
                Role = Role.Client
            };
            ctx.Users.Add(client);
        }

        await ctx.SaveChangesAsync();
    }
}