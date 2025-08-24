using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShop.API.Data;
using MiniShop.API.dto;
using MiniShop.API.Models;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly MiniShopContext _context;

    public AuthController(MiniShopContext context)
    {
        _context = context;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var existing = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existing != null)
        {
            return BadRequest("Email already registered.");
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = hash,
            Role = Role.Client
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok("Registered successfully.");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized("Invalid email or password.");

        var roleString = user.IsAdmin ? "Admin" : "Client";

        return Ok(new
        {
            message = "Login successful",
            id = user.Id,
            username = user.Username,
            email = user.Email,
            isAdmin = user.IsAdmin,
            role = roleString
        });
    }

    [HttpGet]
    public IActionResult GetAll() => Ok(_context.Users.Select(u => new { u.Id, u.Username, u.Email, u.IsAdmin, u.Role }).ToList());
}


