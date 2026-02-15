using Barsoft.SignalRHub.Application.Interfaces;
using Barsoft.SignalRHub.DbWatcher.Workers;
using Barsoft.SignalRHub.Infrastructure.Messaging;
using Barsoft.SignalRHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

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

// ===== RabbitMQ Configuration =====
var rabbitMqSettings = configuration.GetSection(RabbitMqSettings.SectionName).Get<RabbitMqSettings>()
    ?? throw new InvalidOperationException("RabbitMQ settings not found in configuration");

rabbitMqSettings.Validate();

builder.Services.Configure<RabbitMqSettings>(configuration.GetSection(RabbitMqSettings.SectionName));

// Register RabbitMQ publisher as singleton (connection pooling)
builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

// ===== Background Services =====
builder.Services.AddHostedService<ChangeDetectionWorker>();

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

// ===== Build and Run =====
var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Barsoft DB Watcher Service starting...");
logger.LogInformation("Environment: {Environment}", builder.Environment.EnvironmentName);
logger.LogInformation("RabbitMQ: {Host}:{Port}", rabbitMqSettings.Host, rabbitMqSettings.Port);

try
{
    await host.RunAsync();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "DB Watcher Service terminated unexpectedly");
    throw;
}
finally
{
    logger.LogInformation("Barsoft DB Watcher Service stopped");
}
