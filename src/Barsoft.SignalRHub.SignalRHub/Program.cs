using Barsoft.SignalRHub.Application.Interfaces;
using Barsoft.SignalRHub.Infrastructure.Persistence;
using Barsoft.SignalRHub.Infrastructure.Persistence.Repositories;
using Barsoft.SignalRHub.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ===== Configuration =====
var configuration = builder.Configuration;

// ===== Database Configuration =====
builder.Services.AddDbContext<BarsoftDbContext>(options =>
{
    options.UseSqlServer(
        configuration.GetConnectionString("BarsoftDb"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
            sqlOptions.CommandTimeout(30);
        });

    // Development optimizations
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// ===== JWT Authentication Configuration =====
var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT settings not found in configuration");

jwtSettings.Validate();

builder.Services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment(); // Allow HTTP in development
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        ClockSkew = TimeSpan.Zero // Remove default 5 minute clock skew
    };

    // SignalR: Read token from query string for WebSocket connections
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            // If the request is for SignalR hub
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// ===== CORS Configuration =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    // Production: Use specific origins
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("https://yourdomain.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ===== Dependency Injection =====
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// ===== Controllers & API =====
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ===== OpenAPI/Swagger Configuration =====
builder.Services.AddOpenApi();

// ===== Logging Configuration =====
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

if (builder.Environment.IsProduction())
{
    if (OperatingSystem.IsWindows())
    {
        builder.Logging.AddEventLog();
    }
}

// ===== Build Application =====
var app = builder.Build();

// ===== HTTP Request Pipeline =====

// Development middleware
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// CORS
app.UseCors(builder.Environment.IsDevelopment() ? "AllowAll" : "Production");

// Authentication & Authorization (ORDER MATTERS!)
app.UseAuthentication();
app.UseAuthorization();

// Controllers
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Environment = app.Environment.EnvironmentName
})
.WithName("HealthCheck")
.WithOpenApi();

// ===== Run Application =====
app.Logger.LogInformation("Barsoft SignalR Hub API starting...");
app.Logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);

app.Run();
