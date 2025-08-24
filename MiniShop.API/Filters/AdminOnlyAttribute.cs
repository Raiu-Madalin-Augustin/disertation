using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using MiniShop.API.Data;
using MiniShop.API.Models;

namespace MiniShop.API.Filters;

public class AdminOnlyAttribute : ActionFilterAttribute
{
    private readonly MiniShopContext _ctx;
    public AdminOnlyAttribute(MiniShopContext ctx) => _ctx = ctx;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var header = context.HttpContext.Request.Headers["X-User-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(header))
        {
            context.Result = new UnauthorizedObjectResult("Missing X-User-Id header.");
            return;
        }

        if (!int.TryParse(header, out var userId))
        {
            context.Result = new UnauthorizedObjectResult("Invalid X-User-Id.");
            return;
        }

        var isAdmin = _ctx.Users
            .AsNoTracking()
            .Any(u => u.Id == userId && u.Role == Role.Admin);

        if (!isAdmin)
        {
            context.Result = new ObjectResult("Admin privileges required.")
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        base.OnActionExecuting(context);
    }
}
