using Serilog;
using Serilog.Deduplication;
using Serilog.Filters;
using System.Diagnostics;
using System.Security.Principal;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting.WindowsServices;

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

// 🔹 Step 3: Load the JSON configuration file from "C:\Configurations\Specmetrix.json"
builder.Configuration.AddJsonFile(@"C:\Configurations\Specmetrix.json", optional: false, reloadOnChange: true);

// 🔹 Step 4: Retrieve deduplication settings from the configuration
var deduplicationSettings = builder.Configuration.GetSection("Config:Logging:Deduplication").Get<DeduplicationSettings>();

// 🔹 Step 5: Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Filter.With(new DeduplicationFilter(deduplicationSettings))
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// 🔹 Step 6: Register services
builder.Services.AddScoped<ILoggingService, LoggingService>();

// 🔹 Step 7: Configure JSON serialization
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.MapControllers(); // Enable API endpoints

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
    Log.CloseAndFlush(); // Ensure logs are properly written before exit
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
        Verb = "runas", // Request administrator privileges
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
