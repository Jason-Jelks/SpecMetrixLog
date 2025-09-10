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
using LoggingService.Health; // for MongoWriteHealthCheck

var builder = WebApplication.CreateBuilder(args);

// Step 1: Ensure the application is running with Administrator privileges
if (!IsRunningAsAdministrator())
{
    Log.Information("Application is NOT running as Administrator. Attempting to restart with elevated privileges.");
    RestartWithAdminPrivileges();
    return; // Exit the non-elevated instance
}

// Step 2: Ensure it is running as a Windows Service
if (WindowsServiceHelpers.IsWindowsService())
{
    builder.Host.UseWindowsService();
}

// Step 3: Load the JSON configuration file from "C:\\Configurations\\Specmetrix.json"
builder.Configuration.AddJsonFile(@"C:\Configurations\Specmetrix.json", optional: false, reloadOnChange: true);

// Step 4: Configure repository-based MongoDB logging
builder.Services.Configure<Dictionary<string, DatabaseConfig>>(builder.Configuration.GetSection("Databases"));
builder.Services.Configure<RepositoryProfile>(builder.Configuration.GetSection("LoggingRepositoryProfile"));

// Step 5: Register Serilog, ISerilogWrapper, and optional default logger (disabled here)
builder.Services.AddSpecMetrixLogging(builder.Configuration, builder.Environment, registerILoggingService: false);
builder.Host.UseSerilog();

// Step 6: Add MongoDB bootstrap service
builder.Services.AddSingleton<MongoLogService>();

// Step 7: Register your actual log processing background service
builder.Services.AddHostedService<LogProcessingService>();
builder.Services.AddScoped<ILoggingService, LogProcessingService>();

// Step 8: Configure controller JSON options
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Step 9: Health checks (readiness depends on Mongo write ability)
builder.Services.AddHealthChecks()
    .AddCheck<MongoWriteHealthCheck>(
        name: "mongo_write",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "ready" });

var app = builder.Build();

// Ensure MongoDB collection exists on startup
using (var scope = app.Services.CreateScope())
{
    var mongoLogService = scope.ServiceProvider.GetRequiredService<MongoLogService>();
    mongoLogService.EnsureDatabaseAndCollection();
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.MapControllers();

// Health endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = WriteHealthJsonAsync
});

try
{
    Log.Information("Starting SpecMetrix Logging Service with elevated privileges.");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "The application failed to start.");
}
finally
{
    Log.CloseAndFlush();
}

static bool IsRunningAsAdministrator()
{
    using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
    {
        WindowsPrincipal principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}

static void RestartWithAdminPrivileges()
{
    var exePath = Environment.ProcessPath;
    var startInfo = new ProcessStartInfo
    {
        FileName = exePath,
        Verb = "runas",
        UseShellExecute = true
    };

    try
    {
        Process.Start(startInfo);
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
            kvp => kvp.Key,
            kvp => new
            {
                status = kvp.Value.Status.ToString(),
                description = kvp.Value.Description,
                durationMs = (long)kvp.Value.Duration.TotalMilliseconds,
                data = kvp.Value.Data
            })
    };
    return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
}
