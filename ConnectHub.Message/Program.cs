using ConnectHub.Message.Data;
using ConnectHub.Message.Interfaces;
using ConnectHub.Message.Repositories;
using ConnectHub.Message.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Logging Configuration ─────────────────────────────────────────────────────
builder.Logging
    .ClearProviders()
    .AddConsole()
    .AddDebug();

builder.Services.AddLogging(logging =>
{
    logging.SetMinimumLevel(LogLevel.Debug);
    logging.AddFilter("Microsoft.AspNetCore.Authentication", LogLevel.Debug);
});

// ── Database Configuration ────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("MessageDb");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:MessageDb is not configured in appsettings.json");
}

builder.Services.AddDbContext<MessageDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// ── Authentication (for internal service-to-service calls from ChatHub) ───────
var jwtKey = builder.Configuration["Jwt:Key"];
if (!string.IsNullOrWhiteSpace(jwtKey))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
                    logger?.LogWarning("⚠ JWT validation failed: {Message}", context.Exception.Message);
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();
}

// ── CORS Policy (read allowed origins from configuration) ────────────────
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                  ?? (builder.Environment.IsDevelopment()
                        ? new[] { "http://localhost:4200" }
                        : new[] { "http://localhost:5000" });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Content-Disposition");
    });
});

// ── Services ────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddHttpClient();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ── Database Initialization ────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<MessageDbContext>();

    try
    {
        logger.LogInformation("🔄 Applying database migrations for MessageDb...");
        dbContext.Database.Migrate();

        // Seed initial data if empty
        if (!await dbContext.Messages.AnyAsync())
        {
            logger.LogInformation("📝 Seeding initial message data...");
            dbContext.Messages.AddRange(MessageSeedData.SeedMessages);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("✓ Message data seeded");
        }

        logger.LogInformation("✓ Database migrations completed");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "✗ Database initialization failed");
        throw;
    }
}

// ── Middleware Pipeline ────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Message API v1"));
}

app.UseRouting();
app.UseCors("AllowAngularDev");

if (!string.IsNullOrWhiteSpace(jwtKey))
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapControllers();

// ── Startup Log ────────────────────────────────────────────────────────────
var startLogger = app.Services.GetRequiredService<ILogger<Program>>();
startLogger.LogInformation("🚀 ConnectHub.Message started on http://localhost:5002");
startLogger.LogInformation("💾 Database: ConnectHubMessageDb");
startLogger.LogInformation("🔐 JWT authentication: {Status}", 
    string.IsNullOrWhiteSpace(jwtKey) ? "DISABLED" : "ENABLED");

app.Run();
