using Microsoft.AspNetCore.Mvc;
using MiniShop.API.dto;
using System.Text.Json;

namespace MiniShop.Tests;

public class AuthControllerTests
{
    record LoginPayload(int Id, string Username, string Email, bool IsAdmin, string? Role, string? Message);

    [Fact]
    public async Task Login_Should_Succeed_For_Valid_Credentials()
    {
        using var ctx = TestHelpers.CreateContext();
        await TestHelpers.SeedBasicAsync(ctx);
        var ctrl = new AuthController(ctx);

        var result = await ctrl.Login(new LoginRequest
        {
            Email = "admin@example.com",
            Password = "Admin123!"
        });

        var ok = Assert.IsType<OkObjectResult>(result);

        // Serialize then deserialize with case-insensitive matching
        var json = JsonSerializer.Serialize(ok.Value);
        var model = JsonSerializer.Deserialize<LoginPayload>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        Assert.NotNull(model);
        Assert.Equal("admin@example.com", model!.Email);
        Assert.Equal("admin", model.Username);
        Assert.True(model.IsAdmin);
    }

    [Fact]
    public async Task Login_Should_Return_Unauthorized_For_Bad_Password()
    {
        using var ctx = TestHelpers.CreateContext();
        await TestHelpers.SeedBasicAsync(ctx);
        var ctrl = new AuthController(ctx);

        var result = await ctrl.Login(new LoginRequest
        {
            Email = "admin@example.com",
            Password = "WRONG"
        });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_Should_Return_Unauthorized_For_Unknown_Email()
    {
        using var ctx = TestHelpers.CreateContext();
        await TestHelpers.SeedBasicAsync(ctx);
        var ctrl = new AuthController(ctx);

        var result = await ctrl.Login(new LoginRequest
        {
            Email = "nope@example.com",
            Password = "x"
        });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}
