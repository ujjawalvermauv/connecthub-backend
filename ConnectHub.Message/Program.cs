using ConnectHub.Message.Data;
using ConnectHub.Message.Repositories;
using ConnectHub.Message.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("MessageDb");

if (string.IsNullOrWhiteSpace(connectionString))
{
	throw new InvalidOperationException("ConnectionStrings:MessageDb is not configured for ConnectHub.Message.");
}

builder.Services.AddDbContext<MessageDbContext>(options =>
{
	options.UseSqlServer(connectionString);
});

builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IMessageService, MessageService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var dbContext = scope.ServiceProvider.GetRequiredService<MessageDbContext>();
    await dbContext.Database.MigrateAsync();

	if (!await dbContext.Messages.AnyAsync())
	{
		dbContext.Messages.AddRange(MessageSeedData.SeedMessages);
		await dbContext.SaveChangesAsync();
	}
}

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
