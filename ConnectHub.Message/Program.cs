using ConnectHub.Message.Data;
using ConnectHub.Message.Repositories;
using ConnectHub.Message.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<MessageDbContext>(options =>
{
	var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
	if (!string.IsNullOrWhiteSpace(connectionString))
	{
		options.UseSqlServer(connectionString);
		return;
	}

	options.UseInMemoryDatabase("ConnectHubMessageDb");
});

builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IMessageService, MessageService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var dbContext = scope.ServiceProvider.GetRequiredService<MessageDbContext>();
	await dbContext.Database.EnsureCreatedAsync();

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
