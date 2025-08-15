using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using renjibackend.Data;
using renjibackend.Services;
using renjibackend.Utility;
using System.Diagnostics;
using System.Text;


var builder = WebApplication.CreateBuilder(args); // imports web app framework


// Security Middlewares
// CORS setup
builder.Services.AddCors(options =>
{

    options.AddPolicy(name: "ClientPolicy",
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:4200").
                                 WithHeaders("Content-Type", "Authorization", "Accept").
                                 WithMethods("GET", "POST", "PUT", "DELETE").
                                 AllowCredentials();
                      });
});

builder.Services.AddDbContext<RenjiDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           .EnableSensitiveDataLogging() 
           .LogTo(Console.WriteLine, LogLevel.Information));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"])
            ),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

builder.Services.AddScoped<PasswordHashing>();
builder.Services.AddScoped<RenjiDbContext>();
builder.Services.AddScoped<TokenGenerator>();

// Register Services (Dependency Injection)
builder.Services.AddControllers(); // Enables attribute based controllers also the body parsing for API Controllers

var app = builder.Build();

app.UseRouting(); // Enables routing system
app.UseCors("ClientPolicy"); 
app.UseAuthentication(); // Enables attributes like [Authorize], [Anonymous]..
app.UseAuthorization();
app.UseHttpsRedirection(); 
app.MapControllers(); // This binds the routes to the API controllers. Without this, controllers wont handle any API Requests
app.MapGet("/", () => "Server 5101 is running...");
app.Run();


