using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShop.API.Controllers;
using MiniShop.API.dto;

namespace MiniShop.Tests;

public class AdminProductsControllerTests
{
    [Fact]
    public async Task Create_Should_Write_Product()
    {
        using var ctx = TestHelpers.CreateContext();
        await TestHelpers.SeedBasicAsync(ctx);

        var catId = await ctx.Categories.Select(c => c.Id).FirstAsync();
        var ctrl = new AdminProductsController(ctx);

        var http = new DefaultHttpContext();

        http.Request.Headers["X-User-Id"] = "1";


        ctrl.ControllerContext = new ControllerContext { HttpContext = http };

        var res = await ctrl.Create(new CreateProductDto
        {
            Name = "AdminProd",
            Description = "X",
            ImageUrl = null,
            Stock = 9,
            Price = 123.45m,
            CategoryId = catId
        });
        var created = Assert.IsType<CreatedAtActionResult>(res);
        Assert.Equal(201, created.StatusCode ?? 201);


        Assert.True(await ctx.Products.AnyAsync(p => p.Name == "AdminProd"));
    }
}
