using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShop.API.Controllers;
using MiniShop.API.dto;
using MiniShop.API.Models;
using System.Security.Claims;

namespace MiniShop.Tests;

public class AdminCategoriesControllerTests
{
    [Fact]
    public async Task Create_Should_Write_AuditLog()
    {
        using var ctx = TestHelpers.CreateContext();
        await TestHelpers.SeedBasicAsync(ctx);

        var ctrl = new AdminCategoriesController(ctx);

        var http = new DefaultHttpContext();
        http.Request.Headers["X-User-Id"] = "1";

        http.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
        new Claim(ClaimTypes.NameIdentifier, "1"),
        new Claim(ClaimTypes.Role, "Admin")
    }, "TestAuth"));

        ctrl.ControllerContext = new ControllerContext { HttpContext = http };

        var res = await ctrl.Create(new CreateCategoryDto { Name = "Noua" });

        var created = Assert.IsType<CreatedAtActionResult>(res);
        Assert.Equal(201, created.StatusCode ?? 201);

        Assert.True(await ctx.Categories.AnyAsync(c => c.Name == "Noua"));
        Assert.True(await ctx.AuditLogs.AnyAsync(a => a.Action.Contains("CREATE_CATEGORY")));
    }

    [Fact]
    public async Task Delete_Should_Remove_Empty_Category()
    {
        using var ctx = TestHelpers.CreateContext();
        await TestHelpers.SeedBasicAsync(ctx);

        var cat = new Category { Name = "Temp" };
        ctx.Categories.Add(cat);
        await ctx.SaveChangesAsync();

        var ctrl = new AdminCategoriesController(ctx);

        var http = new DefaultHttpContext();
        http.Request.Headers["X-User-Id"] = "1";
        ctrl.ControllerContext = new ControllerContext { HttpContext = http };

        var res = await ctrl.Delete(cat.Id);
        Assert.IsType<NoContentResult>(res);

        Assert.False(await ctx.Categories.AnyAsync(c => c.Id == cat.Id));
    }
}
