using ConnectHub.ChatHub.Data;
using ConnectHub.ChatHub.Hubs;
using ConnectHub.ChatHub.Repositories;
using ConnectHub.ChatHub.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddDbContext<ChatRoomDbContext>(options =>
{
	var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
	if (!string.IsNullOrWhiteSpace(connectionString))
	{
		options.UseSqlServer(connectionString);
		return;
	}

	options.UseInMemoryDatabase("ConnectHubChatRoomDb");
});

builder.Services.AddScoped<IChatRoomRepository, ChatRoomRepository>();
builder.Services.AddScoped<IChatRoomService, ChatRoomService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var dbContext = scope.ServiceProvider.GetRequiredService<ChatRoomDbContext>();
	await dbContext.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
