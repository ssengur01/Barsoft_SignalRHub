using Barsoft.SignalRHub.Application.Interfaces;
using Barsoft.SignalRHub.Infrastructure.Messaging;
using Barsoft.SignalRHub.Infrastructure.Persistence;
using Barsoft.SignalRHub.Infrastructure.Persistence.Repositories;
using Barsoft.SignalRHub.Infrastructure.Security;
using Barsoft.SignalRHub.SignalRHub.BackgroundServices;
using Barsoft.SignalRHub.SignalRHub.Hubs;
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

// ===== RabbitMQ Configuration =====
var rabbitMqSettings = configuration.GetSection(RabbitMqSettings.SectionName).Get<RabbitMqSettings>()
    ?? throw new InvalidOperationException("RabbitMQ settings not found in configuration");

rabbitMqSettings.Validate();

builder.Services.Configure<RabbitMqSettings>(configuration.GetSection(RabbitMqSettings.SectionName));

// ===== SignalR Configuration =====
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.MaximumReceiveMessageSize = 32 * 1024; // 32 KB
});

// ===== Background Services =====
builder.Services.AddHostedService<RabbitMqConsumerService>();

// ===== CORS Configuration =====
builder.Services.AddCors(options =>
{
    // Development: Allow all (SignalR requires AllowCredentials)
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true) // Allow any origin in development
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Required for SignalR
    });

    // Production: Use specific origins
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://45.13.190.248",        // Production server IP
                "http://45.13.190.248:80",     // With explicit port
                "https://yourdomain.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ===== Dependency Injection =====
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IStokHareketRepository, StokHareketRepository>();

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

// CORS must be before authentication (ORDER MATTERS!)
app.UseCors(builder.Environment.IsDevelopment() ? "AllowAll" : "Production");

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
    // HTTPS redirection disabled to allow HTTP API calls
    // app.UseHttpsRedirection();
}

// Authentication & Authorization (ORDER MATTERS!)
app.UseAuthentication();
app.UseAuthorization();

// Controllers
app.MapControllers();

// SignalR Hub
app.MapHub<StokHareketHub>("/hubs/stokhareket");

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
app.Logger.LogInformation("SignalR Hub: /hubs/stokhareket");
app.Logger.LogInformation("RabbitMQ: {Host}:{Port}", rabbitMqSettings.Host, rabbitMqSettings.Port);

app.Run();
