using MiniShop.API.Data;
using Microsoft.EntityFrameworkCore;
using MiniShop.API.Filters;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------- CORS ----------
const string CorsPolicy = "FrontendDev";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
        );
});

// ---------- Services ----------
builder.Services.AddControllers();
builder.Services.AddDbContext<MiniShopContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<AdminOnlyAttribute>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MiniShop API", Version = "v1" });

    // Custom header to impersonate user (X-User-Id)
    c.AddSecurityDefinition("X-User-Id", new OpenApiSecurityScheme
    {
        Description = "Enter the user ID (e.g. 1 for admin, 2 for client).",
        Name = "X-User-Id",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "X-User-Id"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "X-User-Id" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ---------- Middleware order ----------
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS must be BEFORE auth/authorization and BEFORE MapControllers
app.UseCors(CorsPolicy);

// If you add real auth later, keep these after UseCors:
// app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed
using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<MiniShopContext>();
    await MiniShopSeeder.SeedAsync(ctx);
}

app.Run();
