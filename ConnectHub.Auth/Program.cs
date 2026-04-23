using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ConnectHub.Auth.Data;
using ConnectHub.Auth.Services;
using ConnectHub.Auth.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add Services
builder.Services.AddControllers();

//  DB Connection
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//  JWT Authentication
builder.Services.AddAuthentication("Bearer")
.AddJwtBearer("Bearer", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("SuperSecretKey123"))
    };
});

//Authorization
builder.Services.AddAuthorization();

// Register Services
builder.Services.AddScoped<IUserService, UserService>();

// Swagger Configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

//  Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// IMPORTANT ORDER
app.UseAuthentication();   // FIRST
app.UseAuthorization();    // SECOND

app.MapControllers();

app.Run();