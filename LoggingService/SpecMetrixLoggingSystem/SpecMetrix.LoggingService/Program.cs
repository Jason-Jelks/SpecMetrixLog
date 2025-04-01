using Serilog;
using System.Diagnostics;
using System.Security.Principal;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting.WindowsServices;
using LoggingService;
using LoggingService.Configuration;
using LoggingService.Extensions;
using LoggingService.Extensions.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Step 1: Ensure the application is running with Administrator privileges
if (!IsRunningAsAdministrator())
{
    Log.Information("Application is NOT running as Administrator. Attempting to restart with elevated privileges.");
    RestartWithAdminPrivileges();
    return; // Exit the non-elevated instance
}

// 🔹 Step 2: Ensure it is running as a Windows Service
if (WindowsServiceHelpers.IsWindowsService())
{
    builder.Host.UseWindowsService();
}

// 🔹 Step 3: Load the JSON configuration file from "C:\\Configurations\\Specmetrix.json"
builder.Configuration.AddJsonFile(@"C:\Configurations\Specmetrix.json", optional: false, reloadOnChange: true);

// 🔹 Step 4: Configure repository-based MongoDB logging
builder.Services.Configure<Dictionary<string, DatabaseConfig>>(builder.Configuration.GetSection("Databases"));
builder.Services.Configure<RepositoryProfile>(builder.Configuration.GetSection("LoggingRepositoryProfile"));

// 🔹 Step 5: Register Serilog, ISerilogWrapper, and optional default logger (disabled here)
builder.Services.AddSpecMetrixLogging(builder.Configuration, builder.Environment, registerILoggingService: false);
builder.Host.UseSerilog();

// 🔹 Step 6: Add MongoDB bootstrap service
builder.Services.AddSingleton<MongoLogService>();

// 🔹 Step 7: Register your actual log processing background service
builder.Services.AddHostedService<LogProcessingService>();
builder.Services.AddScoped<ILoggingService, LogProcessingService>();

// 🔹 Step 8: Configure controller JSON options
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

// 🔹 Step 9: Ensure MongoDB collection exists on startup
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

/// <summary>
/// Checks if the application is running with Administrator privileges.
/// </summary>
static bool IsRunningAsAdministrator()
{
    using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
    {
        WindowsPrincipal principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}

/// <summary>
/// Attempts to restart the application with elevated privileges.
/// </summary>
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
