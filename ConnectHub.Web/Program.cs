using ConnectHub.Web.Data;
using ConnectHub.Web.Hubs;
using ConnectHub.Web.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// CORS: read allowed origins from configuration
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                  ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Register WebsiteDbContext (separate DB for Website-Controller if needed)
builder.Services.AddDbContext<WebsiteDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("WebsiteDb")));

// Register SignalR
builder.Services.AddSignalR();

// Register your services (IUserService, IMessageService, etc.)
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IChatRoomService, ChatRoomService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddSingleton<IPresenceService, PresenceService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAngularDev");
app.UseAuthorization();

// Map SignalR hub
app.MapHub<ChatHub>("/hubs/chat");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Chat}/{action=Home}/{id?}");

app.Run();
