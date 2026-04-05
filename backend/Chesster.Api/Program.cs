using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using AspNet.Security.OAuth.GitHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Chesster.Api.Data;
using Chesster.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Database=chesster;Username=postgres;Password=postgres";
builder.Services.AddDbContext<ChessterDbContext>(options =>
    options.UseNpgsql(connectionString));

var jwtKey = builder.Configuration["Jwt:Key"] ?? "DefaultSecretKey12345678901234567890";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Chesster";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "Chesster";

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    var key = Encoding.UTF8.GetBytes(jwtKey);
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Cookies["token"] ?? 
                        context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            context.Token = token;
            return Task.CompletedTask;
        }
    };
})
.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
{
    options.ClientId = builder.Configuration["Google:ClientId"] ?? "dummy";
    options.ClientSecret = builder.Configuration["Google:ClientSecret"] ?? "dummy";
    options.CallbackPath = new PathString("/api/auth/google/callback");
    options.SaveTokens = true;
    options.Events = new OAuthEvents
    {
        OnCreatingTicket = context =>
        {
            context.Identity?.AddClaim(new Claim("urn:google:picture", 
                context.User.GetProperty("picture").GetString() ?? ""));
            return Task.CompletedTask;
        }
    };
})
.AddGitHub(options =>
{
    options.ClientId = builder.Configuration["GitHub:ClientId"] ?? "dummy";
    options.ClientSecret = builder.Configuration["GitHub:ClientSecret"] ?? "dummy";
    options.CallbackPath = new PathString("/api/auth/github/callback");
    options.SaveTokens = true;
});

builder.Services.AddAuthorization();

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IChessService, ChessService>();
builder.Services.AddScoped<IPgnParserService, PgnParserService>();
builder.Services.AddScoped<IAnalysisEngine, ChernevAnalysisEngine>();
builder.Services.AddScoped<IGameService, GameService>();

var app = builder.Build();

app.UseCors(options =>
{
    options.WithOrigins("http://localhost:5173", "http://localhost:8080")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ChessterDbContext>();
    db.Database.EnsureCreated();
    
    // Create default demo user if not exists
    var existingUser = db.Users.FirstOrDefault(u => u.Email == "demo@chesster.com");
    if (existingUser == null)
    {
        var demoUser = new Chesster.Api.Models.User
        {
            Username = "demo",
            Email = "demo@chesster.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("demo123")
        };
        db.Users.Add(demoUser);
        await db.SaveChangesAsync();
        Console.WriteLine("Default demo user created: demo@chesster.com / demo123");
    }
}

app.Run();
