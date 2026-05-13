using ConnectHub.ChatHub.Data;
using ConnectHub.ChatHub.Hubs;
using ConnectHub.ChatHub.Interfaces;
using ConnectHub.ChatHub.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
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
    logging.AddFilter("Microsoft.AspNetCore.SignalR", LogLevel.Debug);
    logging.AddFilter("Microsoft.AspNetCore.Http.Connections", LogLevel.Debug);
    logging.AddFilter("Microsoft.AspNetCore.WebSockets", LogLevel.Debug);
    logging.AddFilter("Microsoft.AspNetCore.Authentication", LogLevel.Debug);
});

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ChatHubDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ChatHubDb")));

// ── Authentication ───────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured in appsettings.json");

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

        options.IncludeErrorDetails = true;  // Show detailed error info
        options.SaveToken = true;  // Save token in context for access in other middleware

        // SignalR WebSocket limitation: JWT must be in query string ?access_token=...
        // Cannot use Authorization header with WebSocket connections
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
                    logger?.LogInformation("✓ [JWT] SignalR JWT extracted from query string | Path: {Path} | TokenLength: {TokenLength}", 
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
                
                logger?.LogError("✗ [JWT] Authentication failed for {Path} | Exception: {ExceptionType} | Message: {Message}", 
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
                
                logger?.LogInformation("✓ [JWT] Token validated successfully | Path: {Path} | UserId: {UserId} | UserName: {UserName} | Email: {Email}",
                    path, userIdentifier ?? "NULL", userName ?? "NULL", email ?? "NULL");
                
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
                logger?.LogError("✗ [JWT] Authorization challenge issued | Path: {Path} | Error: {Error} | Description: {Description}",
                    context.HttpContext.Request.Path,
                    context.Error ?? "UNKNOWN",
                    context.ErrorDescription ?? "No description");
                
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpClient();

// ── SignalR Configuration ─────────────────────────────────────────────────────
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumParallelInvocationsPerClient = 1;
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
})
.AddMessagePackProtocol();

// Map JWT 'sub' claim → SignalR Context.UserIdentifier
builder.Services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider,
    NameIdentifierUserIdProvider>();

// ── Services (Singletons for connection tracking) ────────────────────────────
builder.Services.AddSingleton<IPresenceService, PresenceService>();
builder.Services.AddSingleton<IUserConnectionManager, UserConnectionManager>();

// ── CORS Policy (Allow frontend on all development ports + production) ────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        // Development: localhost:4200 (original), 4201, 4202 (current), 65320
        // Production: configure based on environment
        var origins = builder.Environment.IsDevelopment()
           ? new[]
{
    "http://localhost:4200",
    "http://localhost:4201",
    "http://localhost:4202",
    "http://localhost:64817"
}
            : new[] { "https://connecthub.example.com" }; // Update with production domain

        policy.WithOrigins(origins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials() // REQUIRED for SignalR
              .WithExposedHeaders("Content-Disposition");
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ── Database Initialization ───────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<ChatHubDbContext>();
    
    try
    {
        logger.LogInformation("🔄 Applying database migrations for ChatHubDb...");
        db.Database.Migrate();
        logger.LogInformation("✓ Database migrations completed");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "✗ Database migration failed");
        throw;
    }
}

// ── Middleware Pipeline ──────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ChatHub API v1"));
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
app.MapHub<ChatHub>("/hubs/chat");

// Start up logs
var startLogger = app.Services.GetRequiredService<ILogger<Program>>();
startLogger.LogInformation("🚀 ConnectHub.ChatHub started on http://localhost:5003");
startLogger.LogInformation("📡 SignalR Hub endpoint: /hubs/chat");
startLogger.LogInformation("🔐 JWT Bearer authentication: ENABLED (via [Authorize] on Hub class)");
startLogger.LogInformation("✓ CORS enabled for: http://localhost:4200, 4201, 4202");
startLogger.LogInformation("✓ OnMessageReceived handler active for JWT query parameter extraction");

app.Run();
