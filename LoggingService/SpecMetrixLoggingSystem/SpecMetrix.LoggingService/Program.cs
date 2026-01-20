using Serilog;
using System.Diagnostics;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting.WindowsServices;
using LoggingService;
using LoggingService.Configuration;
using LoggingService.Extensions;
using LoggingService.Extensions.Interfaces;
using LoggingService.Health; // MongoWriteHealthCheck
using SpecMetrix.LoggingService.Services; // LoggingQueueService, LoggingIngestionOptions, LoggingStorageInitializer

var builder = WebApplication.CreateBuilder(args);

// --- Elevation: require admin for service cert & reserved ports ---
if (!IsRunningAsAdministrator())
{
    Log.Information("Application is NOT running as Administrator. Attempting to restart with elevated privileges.");
    RestartWithAdminPrivileges();
    return; // exit non-elevated instance
}

// --- Run as Windows Service when hosted that way ---
if (WindowsServiceHelpers.IsWindowsService())
{
    builder.Host.UseWindowsService();
}

// --- Configuration: external JSON (mongo + repo profiles, etc.) ---
builder.Configuration.AddJsonFile(
    @"C:\Configurations\Specmetrix.json",
    optional: false,
    reloadOnChange: true);

// --- Bind repo/database config for Mongo ---
builder.Services.Configure<Dictionary<string, DatabaseConfig>>(builder.Configuration.GetSection("Databases"));
builder.Services.Configure<RepositoryProfile>(builder.Configuration.GetSection("LoggingRepositoryProfile"));

// --- Serilog & logging helpers (no legacy ILoggingService registration here) ---
builder.Services.AddSpecMetrixLogging(builder.Configuration, builder.Environment, registerILoggingService: false);
builder.Host.UseSerilog();

// --- Mongo bootstrap & write verifier (used by health checks) ---
builder.Services.AddSingleton<MongoLogService>();
builder.Services.AddSingleton<IMongoWriteVerifier, MongoLogService>();

// --- Ingestion pipeline options (DI-configurable) ---
builder.Services.Configure<LoggingIngestionOptions>(builder.Configuration.GetSection("Config:Logging:Ingestion"));

// --- Queue-based ingestion pipeline: ONE hosted service ---
builder.Services.AddSingleton<LoggingQueueService>();
builder.Services.AddSingleton<ILoggingService>(sp => sp.GetRequiredService<LoggingQueueService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<LoggingQueueService>());

// --- Deterministic Mongo init ONCE at startup ---
builder.Services.AddHostedService<LoggingStorageInitializer>();

// --- Controllers & JSON enum casing ---
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// --- Health (readiness = can write to Mongo) ---
builder.Services.AddHealthChecks()
    .AddCheck<MongoWriteHealthCheck>(
        name: "mongo_write",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "ready" });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Map controllers (LogController -> /api/log)
app.MapControllers();

// Health endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = WriteHealthJsonAsync
});

try
{
    Log.Information("Starting SpecMetrix Logging Service with elevated privileges.");
    app.Run();   // Kestrel endpoints are bound via appsettings.json (e.g., https://127.0.0.1:5777)
}
catch (Exception ex)
{
    Log.Fatal(ex, "The SpecMetrix Logging Service failed to start.");
}
finally
{
    Log.CloseAndFlush();
}

// ---------------- helpers ----------------

static bool IsRunningAsAdministrator()
{
    using (var identity = WindowsIdentity.GetCurrent())
    {
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}

static void RestartWithAdminPrivileges()
{
    var exePath = Environment.ProcessPath;
    var psi = new ProcessStartInfo
    {
        FileName = exePath,
        Verb = "runas",
        UseShellExecute = true
    };

    try
    {
        Process.Start(psi);
        Log.Information("Restarting SpecMetrix Logging Service with Administrator privileges.");
    }
    catch
    {
        Log.Error("User denied admin privileges. SpecMetrix Logging Service will not start.");
    }
}

static Task WriteHealthJsonAsync(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    var payload = new
    {
        status = report.Status.ToString(),
        totalDurationMs = (long)report.TotalDuration.TotalMilliseconds,
        entries = report.Entries.ToDictionary(
            e => e.Key,
            e => new
            {
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                durationMs = (long)e.Value.Duration.TotalMilliseconds,
                data = e.Value.Data
            })
    };
    return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
}
