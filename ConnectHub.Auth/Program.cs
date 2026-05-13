using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Linq;
using ConnectHub.Auth.Data;
using ConnectHub.Auth.Services;
using ConnectHub.Auth.Interfaces;
using ConnectHub.Auth.Models;

var builder = WebApplication.CreateBuilder(args);

// ✅ Add Services
builder.Services.AddControllers();

// ✅ DB Connection
var connectionString = builder.Configuration.GetConnectionString("AuthDb");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:AuthDb is not configured for ConnectHub.Auth.");
}

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(connectionString));

// ✅ JWT Authentication
var secretKey = "ThisIsAReallyStrongSecretKeyForJWTAuth123456";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

builder.Services.AddSingleton<SecurityKey>(signingKey);

builder.Services.AddAuthentication("Bearer")
.AddJwtBearer("Bearer", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = signingKey
    };
});

// ✅ Authorization
builder.Services.AddAuthorization();

// ✅ 🔥 CORS (THIS WAS MISSING)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ✅ Register Services
builder.Services.AddScoped<IUserService, UserService>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Apply migrations and seed data
try
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        
        try
        {
            await dbContext.Database.MigrateAsync();
            Console.WriteLine("✅ Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Database migration failed: {ex.Message}");
            Console.WriteLine("Attempting to create database...");
            try
            {
                await dbContext.Database.EnsureCreatedAsync();
                Console.WriteLine("✅ Database created successfully.");
            }
            catch (Exception createEx)
            {
                Console.WriteLine($"❌ Failed to create database: {createEx.Message}");
                throw;
            }
        }

        if (!await dbContext.Users.AnyAsync(u => u.Email == "shubham@gmail.com"))
        {
            var hasher = new PasswordHasher<User>();
            var seedUser = new User
            {
                UserName = "shubham",
                DisplayName = "Shubham",
                Email = "shubham@gmail.com",
                Role = "User",
                IsActive = true,
                IsOnline = false,
                CreatedAt = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow
            };

            seedUser.PasswordHash = hasher.HashPassword(seedUser, "1234567890");
            dbContext.Users.Add(seedUser);
        }

        if (!await dbContext.Users.AnyAsync(u => u.Email == "sarthak@gmail.com"))
        {
            var hasher = new PasswordHasher<User>();
            var seedUser = new User
            {
                UserName = "sarthak",
                DisplayName = "Sarthak",
                Email = "sarthak@gmail.com",
                Role = "User",
                IsActive = true,
                IsOnline = false,
                CreatedAt = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow
            };

            seedUser.PasswordHash = hasher.HashPassword(seedUser, "1234567890");
            dbContext.Users.Add(seedUser);
        }

        // Normalize existing users that have missing/empty emails so future logins by email work
        var usersToFix = await dbContext.Users.Where(u => u.Email == null || u.Email == "" || u.Email.Trim() == "").ToListAsync();
        if (usersToFix.Any())
        {
            foreach (var u in usersToFix)
            {
                // prefer userName based email, fallback to a generic placeholder
                var name = string.IsNullOrWhiteSpace(u.UserName) ? $"user_{u.UserId}" : u.UserName;
                u.Email = $"{name}@example.com";
            }
            Console.WriteLine($"Normalized {usersToFix.Count} users with missing emails.");
        }

        await dbContext.SaveChangesAsync();
        Console.WriteLine("✅ Database seeding completed.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Critical error during startup: {ex.Message}");
    // Re-throw to fail startup if database creation ultimately failed
    throw;
}

// ✅ Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

// 🔥 VERY IMPORTANT ORDER
app.UseCors("AllowAngularDev");   // ✅ ADD THIS LINE HERE

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles(new StaticFileOptions
{
    RequestPath = "/api/users"
});

app.MapControllers();

app.Run();