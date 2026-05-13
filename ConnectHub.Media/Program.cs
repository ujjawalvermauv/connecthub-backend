using ConnectHub.Media.Data;
using ConnectHub.Media.Repositories;
using ConnectHub.Media.Services;
using ConnectHub.Media.Options;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5005");
builder.WebHost.UseWebRoot("wwwroot");

var webRootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(webRootPath);
Directory.CreateDirectory(Path.Combine(webRootPath, "uploads"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✅ PHASE 1: Add CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
    policy =>
    {
        policy.WithOrigins(
            "http://localhost:4200",
            "http://localhost:4201",
            "http://localhost:4202")
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
    options.UseSqlServer(mediaDbConnectionString);
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
