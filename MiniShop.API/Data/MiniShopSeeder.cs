// MiniShop.API/Data/MiniShopSeeder.cs
using MiniShop.API.Models;
using Microsoft.EntityFrameworkCore;

namespace MiniShop.API.Data;

public static class MiniShopSeeder
{
    public static async Task SeedAsync(MiniShopContext ctx)
    {
        await ctx.Database.MigrateAsync();

        // 1) Asigurăm categoriile (idempotent)
        var categoriesWanted = new[]
        {
            "Electronice",
            "Cărţi",
            "Îmbrăcăminte",
            "Casă & Bucătărie"
        };

        var existingCats = await ctx.Categories.ToListAsync();
        foreach (var c in categoriesWanted)
        {
            if (!existingCats.Any(x => x.Name == c))
            {
                ctx.Categories.Add(new Category { Name = c });
            }
        }
        await ctx.SaveChangesAsync();

        // map rapid: nume categorie -> Id
        var cats = await ctx.Categories.ToDictionaryAsync(c => c.Name, c => c.Id);

        // 2) UPSET users (admin + client demo) – idempotent
        await UpsertUserAsync(ctx,
            email: "admin@example.com",
            username: "admin",
            role: Role.Admin,
            isAdmin: true,
            plainPassword: "Admin123!"
        );

        await UpsertUserAsync(ctx,
            email: "alice@example.com",
            username: "alice",
            role: Role.Client,
            isAdmin: false,
            plainPassword: "Pa$$w0rd"
        );

        // 3) Produse (~32) – idempotent (după Name + CategoryId)
        var products = new List<Product>
        {
            // Electronice (12)
            P("Căşti wireless",          "Bluetooth 5.0, autonomie 30h",          25, 199.99m, cats["Electronice"]),
            P("Mouse gaming",            "16000 DPI, RGB",                        40, 149.99m, cats["Electronice"]),
            P("Tastatură mecanică",      "Switch-uri tactile, layout RO",         15, 349.99m, cats["Electronice"]),
            P("Monitor 27'' 144Hz",      "IPS, 1ms, G-Sync compat.",              12, 1199.00m, cats["Electronice"]),
            P("SSD NVMe 1TB",            "PCIe 4.0, 7GB/s",                        30, 399.90m, cats["Electronice"]),
            P("HDD 4TB",                 "3.5'' 5400rpm",                          20, 349.90m, cats["Electronice"]),
            P("Boxă portabilă",          "Waterproof IPX7, 20W",                   22, 259.50m, cats["Electronice"]),
            P("Cameră web 1080p",        "Microfon dual, autofocus",               28, 199.00m, cats["Electronice"]),
            P("Microfon USB",            "Cardioid, studio entry",                 18, 329.00m, cats["Electronice"]),
            P("Încărcător GaN 65W",      "USB-C PD, compact",                      35, 189.90m, cats["Electronice"]),
            P("Router Wi‑Fi 6",          "AX3000, OFDMA",                          16, 449.00m, cats["Electronice"]),
            P("Hub USB‑C 7‑in‑1",        "HDMI, USB3, card reader",                26, 219.00m, cats["Electronice"]),

            // Cărţi (8)
            P("Clean Code",              "Robert C. Martin",                       14, 129.00m, cats["Cărţi"]),
            P("Design Patterns",         "GoF – Elemente reutilizabile OOP",      10, 169.00m, cats["Cărţi"]),
            P("Refactoring",             "Martin Fowler, a 2‑a ediție",           12, 159.00m, cats["Cărţi"]),
            P("You Don't Know JS",       "Kyle Simpson (seria)",                   9,   99.00m, cats["Cărţi"]),
            P("Deep Work",               "Cal Newport",                            11,  89.00m, cats["Cărţi"]),
            P("Atomic Habits",           "James Clear",                            13,  99.00m, cats["Cărţi"]),
            P("Cracking the Coding Int.", "McDowell – interviuri",                 8,  179.00m, cats["Cărţi"]),
            P("SQL Antipatterns",        "Bill Karwin",                            7,  139.00m, cats["Cărţi"]),

            // Îmbrăcăminte (6)
            P("Hanorac unisex",          "Bumbac 80%, gri",                        20, 159.00m, cats["Îmbrăcăminte"]),
            P("Tricou alb",              "Bumbac 100%, logo minimal",              35,  69.90m, cats["Îmbrăcăminte"]),
            P("Geacă windbreaker",       "Rezistent la apă, negru",                10, 249.00m, cats["Îmbrăcăminte"]),
            P("Pantaloni jogger",        "Slim fit, bleumarin",                    18, 139.00m, cats["Îmbrăcăminte"]),
            P("Șapcă snapback",          "Broderie, negru",                        25,  59.90m, cats["Îmbrăcăminte"]),
            P("Ciorapi tehnici",         "Respirabili, 3 perechi",                 30,  39.90m, cats["Îmbrăcăminte"]),

            // Casă & Bucătărie (6)
            P("Fierbător apă",           "1.7L, auto‑stop",                        18,  89.90m, cats["Casă & Bucătărie"]),
            P("Set cuțite inox",         "6 piese + suport",                       12, 129.00m, cats["Casă & Bucătărie"]),
            P("Mixer de mână",           "5 viteze + turbo",                       15, 119.00m, cats["Casă & Bucătărie"]),
            P("Aspirator vertical",      "2‑în‑1, fără sac",                       9,  499.00m, cats["Casă & Bucătărie"]),
            P("Cafetiere filtru",        "Programabilă, 1.25L",                    11, 189.90m, cats["Casă & Bucătărie"]),
            P("Set pahare 6x",           "Sticlă termorezistentă",                 20,  79.90m, cats["Casă & Bucătărie"]),
        };

        // insert numai dacă nu există (după Name + CategoryId)
        foreach (var p in products)
        {
            bool exists = await ctx.Products.AnyAsync(x => x.Name == p.Name && x.CategoryId == p.CategoryId);
            if (!exists)
            {
                ctx.Products.Add(p);
            }
        }

        await ctx.SaveChangesAsync();
    }

    private static Product P(string name, string? desc, int stock, decimal price, int categoryId)
        => new Product
        {
            Name = name,
            Description = desc,
            Stock = stock,
            Price = price,
            CategoryId = categoryId
        };

    private static async Task UpsertUserAsync(
        MiniShopContext ctx,
        string email,
        string username,
        Role role,
        bool isAdmin,
        string plainPassword)
    {
        var u = await ctx.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (u is null)
        {
            u = new User
            {
                Email = email,
                Username = username,
                Role = role,
                IsAdmin = isAdmin,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword)
            };
            ctx.Users.Add(u);
        }
        else
        {
            // păstrăm parola existentă; doar corectăm metadatele
            u.Username = string.IsNullOrWhiteSpace(u.Username) ? username : u.Username;
            u.Role = role;
            u.IsAdmin = isAdmin;
            ctx.Users.Update(u);
        }
        await ctx.SaveChangesAsync();
    }
}
