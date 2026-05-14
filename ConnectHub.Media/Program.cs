using ConnectHub.Media.Data;
using ConnectHub.Media.Repositories;
using ConnectHub.Media.Services;
using ConnectHub.Media.Options;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = "wwwroot"
});

// Read media listen URL from config (fallback to http://localhost:5005)
var mediaUrl = builder.Configuration["ServiceUrls:MediaUrl"] ?? "http://localhost:5005";
builder.WebHost.UseUrls(mediaUrl);

var webRootPath = builder.Environment.WebRootPath;
Directory.CreateDirectory(webRootPath);
Directory.CreateDirectory(Path.Combine(webRootPath, "uploads"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✅ PHASE 1: Add CORS configuration (read allowed origins from config)
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                  ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
    policy =>
    {
        policy.WithOrigins(corsOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

var mediaDbConnectionString = builder.Configuration.GetConnectionString("MediaDb");
if (string.IsNullOrWhiteSpace(mediaDbConnectionString))
{
    throw new InvalidOperationException("ConnectionStrings:MediaDb is not configured for ConnectHub.Media.");
}

builder.Services.AddDbContext<MediaDbContext>(options =>
{
    options.UseNpgsql(mediaDbConnectionString);
});

builder.Services.Configure<AzureBlobOptions>(builder.Configuration.GetSection("AzureBlob"));
builder.Services.Configure<MediaStorageOptions>(builder.Configuration.GetSection("MediaStorage"));
builder.Services.AddScoped<IMediaRepository, MediaRepository>();
builder.Services.AddScoped<IBlobStorageGateway>(serviceProvider =>
{
    var storageOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MediaStorageOptions>>().Value;

    if (string.Equals(storageOptions.Provider, "Azure", StringComparison.OrdinalIgnoreCase))
    {
        return new AzureBlobStorageGateway(serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AzureBlobOptions>>());
    }

    return new LocalBlobStorageGateway(
        serviceProvider.GetRequiredService<IWebHostEnvironment>(),
        serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MediaStorageOptions>>(),
        serviceProvider.GetRequiredService<ILogger<LocalBlobStorageGateway>>());
});
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddHostedService<MediaCleanupHostedService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
    await dbContext.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();

// ✅ PHASE 1: Enable CORS middleware (must be before routing/auth)
app.UseRouting();
app.UseCors("AllowAngular");

app.MapControllers();

app.Run();
