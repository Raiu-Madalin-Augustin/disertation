using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MiniShop.API.Controllers;
using MiniShop.API.Data;

namespace MiniShop.Tests
{
    public class OrdersControllerTests
    {
        private record OrderItemRow(int Id, int ProductId, int Quantity, decimal Price);
        private record OrderRow(int Id, int UserId, DateTime CreatedAt, List<OrderItemRow> Items, decimal Total);

        private static MiniShopContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<MiniShopContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new MiniShopContext(options);
        }

        [Fact]
        public async Task Place_Should_Return_BadRequest_If_Cart_Empty()
        {
            using var ctx = CreateContext();
            await TestHelpers.SeedBasicAsync(ctx);

            var userId = await ctx.Users.Where(u => !u.IsAdmin).Select(u => u.Id).FirstAsync();
            var ctrl = new OrdersController(ctx);

            var res = await ctrl.Place(userId);

            var bad = Assert.IsType<BadRequestObjectResult>(res);
            Assert.Contains("Cart is empty", bad.Value!.ToString());
        }
        [Fact]
        public async Task Place_Should_Return_Conflict_If_Insufficient_Stock()
        {
            using var ctx = CreateContext();
            await TestHelpers.SeedBasicAsync(ctx);

            var userId = await ctx.Users.Where(u => !u.IsAdmin).Select(u => u.Id).FirstAsync();
            var prod = await ctx.Products.FirstAsync();
            await TestHelpers.AddCartItem(ctx, userId, prod.Id, 1000);

            var ctrl = new OrdersController(ctx);
            var res = await ctrl.Place(userId);

            var conflict = Assert.IsType<ConflictObjectResult>(res);  
            Assert.Equal(409, conflict.StatusCode ?? 409);
        }

        [Fact]
        public async Task GetByUser_Should_Return_Orders_With_Items()
        {
            using var ctx = CreateContext();
            await TestHelpers.SeedBasicAsync(ctx);

            var userId = await ctx.Users.Where(u => !u.IsAdmin).Select(u => u.Id).FirstAsync();
            var prod = await ctx.Products.FirstAsync();
            await TestHelpers.AddCartItem(ctx, userId, prod.Id, 2);

            var ctrl = new OrdersController(ctx);
            await ctrl.Place(userId);

            var res = await ctrl.GetByUser(userId);
            var ok = Assert.IsType<OkObjectResult>(res);

            var json = JsonSerializer.Serialize(ok.Value);
            var orders = JsonSerializer.Deserialize<List<OrderRow>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            Assert.NotNull(orders);
            Assert.Single(orders!);
            Assert.NotNull(orders![0].Items);
            Assert.True(orders![0].Items.Count > 0);
        }
    }
}
