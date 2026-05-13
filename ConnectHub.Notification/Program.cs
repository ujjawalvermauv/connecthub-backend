using ConnectHub.Notification.Data;
using ConnectHub.Notification.Models;
using ConnectHub.Notification.Hubs;
using ConnectHub.Notification.Repositories;
using ConnectHub.Notification.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ── Logging Configuration ─────────────────────────────────────────────────────
builder.Logging
    .ClearProviders()
    .AddConsole()
    .AddDebug();

builder.Services.AddLogging(logging =>
{
    logging.SetMinimumLevel(LogLevel.Debug);
    logging.AddFilter("Microsoft.AspNetCore.SignalR", LogLevel.Debug);
    logging.AddFilter("Microsoft.AspNetCore.Http.Connections", LogLevel.Debug);
    logging.AddFilter("Microsoft.AspNetCore.WebSockets", LogLevel.Debug);
    logging.AddFilter("Microsoft.AspNetCore.Authentication", LogLevel.Debug);
});

// ── CORS Policy ─────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        // Development: Add all frontend ports (4200, 4201, 4202, etc.)
        var origins = builder.Environment.IsDevelopment()
            ? new[] { "http://localhost:4200", "http://localhost:4201", 
                      "http://localhost:4202", "http://localhost:65320" }
            : new[] { "https://connecthub.example.com" };

        policy.WithOrigins(origins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials() // REQUIRED for SignalR
              .WithExposedHeaders("Content-Disposition");
    });
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── SignalR Configuration ─────────────────────────────────────────────────────
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumParallelInvocationsPerClient = 1;
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
})
.AddMessagePackProtocol();

// ── Authentication ───────────────────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException("Jwt:Key is not configured in appsettings.json");
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };

        // SignalR WebSocket limitation: JWT must be in query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
                var accessTokenValue = context.Request.Query["access_token"].ToString();
                var path = context.HttpContext.Request.Path;
                var method = context.HttpContext.Request.Method;
                
                logger?.LogDebug("[JWT] OnMessageReceived: Path={Path}, Method={Method}, HasToken={HasToken}",
                    path, method, !string.IsNullOrEmpty(accessTokenValue));
                
                if (!string.IsNullOrEmpty(accessTokenValue) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessTokenValue;
                    logger?.LogInformation("✓ [JWT] NotificationHub JWT extracted from query string | Path: {Path} | TokenLength: {TokenLength}",
                        path, accessTokenValue.Length);
                }
                else if (!string.IsNullOrEmpty(accessTokenValue))
                {
                    logger?.LogWarning("⚠ [JWT] Token present but path not /hubs: {Path}", path);
                }
                else
                {
                    logger?.LogWarning("⚠ [JWT] No token in query string for path: {Path}", path);
                }
                
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
                var path = context.HttpContext.Request.Path;
                var exceptionType = context.Exception?.GetType().Name ?? "Unknown";
                
                logger?.LogError("✗ [JWT] NotificationHub Authentication failed for {Path} | Exception: {ExceptionType} | Message: {Message}",
                    path, exceptionType, context.Exception?.Message ?? "No message");
                
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
                var userIdentifier = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var userName = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
                var email = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                var path = context.HttpContext.Request.Path;
                
                logger?.LogInformation("✓ [JWT] NotificationHub Token validated successfully | Path: {Path} | UserId: {UserId} | UserName: {UserName} | Email: {Email}",
                    path, userIdentifier ?? "NULL", userName ?? "NULL", email ?? "NULL");
                
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
                logger?.LogError("✗ [JWT] NotificationHub Authorization challenge issued | Path: {Path} | Error: {Error} | Description: {Description}",
                    context.HttpContext.Request.Path,
                    context.Error ?? "UNKNOWN",
                    context.ErrorDescription ?? "No description");
                
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ── Database Configuration ──────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("NotificationDb");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:NotificationDb is not configured in appsettings.json");
}

builder.Services.AddDbContext<NotificationDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

// ── SMTP Email Configuration ────────────────────────────────────────────────
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));

// ── Services ────────────────────────────────────────────────────────────────
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<IPresenceService, PresenceService>();
builder.Services.AddSingleton<IUserIdProvider, NameIdentifierUserIdProvider>();
builder.Services.AddHttpClient();

var app = builder.Build();

// ── Database Initialization ────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    
    try
    {
        logger.LogInformation("🔄 Applying database migrations for NotificationDb...");
        dbContext.Database.MigrateAsync().Wait();
        logger.LogInformation("✓ Database migrations completed");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "✗ Database migration failed");
        throw;
    }
}

// ── Middleware Pipeline ────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Notification API v1"));
}

// ✅ CRITICAL: Enable WebSocket support for SignalR
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(15)
});

app.UseRouting();
app.UseCors("AllowAngularDev");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

// ── Startup Log ────────────────────────────────────────────────────────────
var startLogger = app.Services.GetRequiredService<ILogger<Program>>();
startLogger.LogInformation("🚀 ConnectHub.Notification started on http://localhost:5004");
startLogger.LogInformation("📡 SignalR Hub endpoint: /hubs/notifications");
startLogger.LogInformation("🔐 JWT Bearer authentication: ENABLED (via [Authorize] on Hub class)");
startLogger.LogInformation("✓ OnMessageReceived handler active for JWT query parameter extraction");
startLogger.LogInformation("📧 SMTP Email notifications: CONFIGURED");

app.Run();