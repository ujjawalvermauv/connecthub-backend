using ConnectHub.Media.Data;
using ConnectHub.Media.Repositories;
using ConnectHub.Media.Services;
using ConnectHub.Media.Options;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
builder.Services.AddScoped<IMediaRepository, MediaRepository>();
builder.Services.AddScoped<IBlobStorageGateway, AzureBlobStorageGateway>();
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

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
