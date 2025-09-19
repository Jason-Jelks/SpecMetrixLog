# SpecMetrixLog

SpecMetrix Logging System written in .NET 8, designed to accept event messages/logs from SpecMetrix and store them in MongoDB.  
This logging system is built for high-speed event processing, deduplication, and efficient storage.

## Features & Enhancements

### Core Functionalities
- Receives logs from SpecMetrix and writes to MongoDB.
- Deduplication filter to prevent duplicate high-speed logs from flooding the system.
- Caching system for fast retrieval of recent events.
- Automated MongoDB collection setup for optimal performance.

### New Features & Improvements
- Cross-Platform Support:  
  Optimized for Windows/Linux/macOS.  
  Runs as a Windows Service if deployed on Windows.  

- Configuration Handling:  
  Loads settings from C:\Configurations\specmetrix.json.  
  Automated MongoDB Time-Series Collection Creation.  
  Dynamic retention settings for logs (configurable purge duration).  

- Optimized Logging with Serilog:  
  Supports MongoDB logging.  
  No duplicate console logs (prevents redundant output).  
  Writes structured logs to MongoDB for easy querying.  
  Startup & shutdown logs explicitly handled for diagnostics.  

- Secure HTTPS Support:  
  Reads TLS settings from specmetrix.json.  
  Auto-configures Kestrel for secure API access.  

- Repository-Based Configuration:  
  Supports named database configurations in "Databases" section.  
  Uses "LoggingRepositoryProfile" to dynamically select primary/secondary database.  
  Enables "PrimaryOnly" and "Failover" modes for resilient logging.  

- LoggingService.Extensions shared package:  
  Reusable logging abstraction (ILoggingService, ISerilogWrapper) for all SpecMetrix microservices.  
  Supports centralized Serilog configuration via .AddSpecMetrixLogging() extension.  

- Bootstrap support for legacy services:  
  .NET Framework 4.7.2 services (DataService, SensorService, etc.) can now call  
  Logging.InitializeClient(baseUrl, healthPath, logPath, fallbackFile) to enable health-checked, failover logging.  
  Fully compatible with .NET 8 microservices.  

## Solution Overview

The SpecMetrix logging solution consists of three key components:

1. MongoDB Data Service  
   A separate generic service to read/write logs to MongoDB.  
   Automated Time-Series Collection Setup for logs.  
   Configurable log retention period (defaults to 7 days via TTL settings).  
   Optimized queries for fast log retrieval.  
   Failover support via LoggingRepositoryProfile using named MongoDB configs.  

2. SpecMetrixLog Service  
   Runs as a background service (Windows/Linux compatible).  
   Processes incoming logs from SpecMetrix.  
   Employs deduplication filter for high-speed log ingestion.  
   Uses caching to avoid unnecessary database reads.  
   Automatically ensures MongoDB database and collection exist.  

3. LoggingService.Extensions (.NET Library)  
   Shared class library used across all microservices.  
   Contains:  
   - ILoggingService interface  
   - ISerilogWrapper abstraction  
   - DI extension: .AddSpecMetrixLogging(configuration)  
   Distributed via internal NuGet package or local project reference.  

## Configuration Files

### specmetrix.json (Primary Config)
Located at C:\Configurations\specmetrix.json.  
Defines:  
- Logging settings (MongoDB, Console, Deduplication, etc.)  
- MongoDB Time-Series Settings:  
  - Granularity: Set as hours, minutes, or seconds.  
  - ExpireAfterDays: Defines automatic log retention.  
- LoggingRepositoryProfile:  
  - Primary, Secondary, Mode: supports "PrimaryOnly" and "Failover" modes.  
- TLS/HTTPS certificates for secure communication.  

## Usage Examples

### .NET Framework 4.7.2 Windows Service

Inside OnStart() of your service (e.g., SensorService or DataService):

```csharp
protected override void OnStart(string[] args)
{
    // 1. Initialize legacy event sources
    Logging.Initialize();

    // 2. Configure LoggingService client from App.config
    var baseUrl      = ConfigurationManager.AppSettings["LoggingServiceBaseUrl"]    ?? "https://127.0.0.1:5777";
    var healthPath   = ConfigurationManager.AppSettings["LoggingServiceHealthPath"] ?? "/health";
    var logPath      = ConfigurationManager.AppSettings["LoggingServiceLogPath"]    ?? "/api/log";
    var fallbackFile = ConfigurationManager.AppSettings["LoggingServiceFallbackFile"] 
                       ?? @"C:\Data\Logs\SpecMetrixLog_Fallback.ndjson";

    Logging.InitializeClient(baseUrl, healthPath, logPath, fallbackFile);

    Logging.Write(LogLevel.Information,
                  "Service startup complete.",
                  new Dictionary<string, object>(),
                  process: "SA.Service",
                  category: LogCategory.ServiceOperation);
}
```

This ensures health-checked logging:  
- Posts to LoggingService if online.  
- Falls back to file if MongoDB/LoggingService is unavailable.  

### .NET 8 Microservice

In Program.cs:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register logging via shared extensions
builder.Services.AddSpecMetrixLogging(builder.Configuration);

// Add services
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
```

Usage inside any class:

```csharp
public class MyController : ControllerBase
{
    private readonly ILoggingService _logging;

    public MyController(ILoggingService logging)
    {
        _logging = logging;
    }

    [HttpGet("/test")]
    public IActionResult Test()
    {
        _logging.EnqueueLog(new LogEntry
        {
            Level = LogLevel.Information,
            MessageTemplate = "Test endpoint hit at {time}",
            TemplateValues = new Dictionary<string, object> { { "time", DateTime.UtcNow } },
            Process = "MyService",
            Category = LogCategory.UI
        });

        return Ok("Logged!");
    }
}
```

## Notes & Considerations

- Logging Redundancy Prevention:  
  Console logs are disabled if MongoDB logging is enabled to prevent duplicate logs.  

- Windows Service Optimization:  
  The application automatically detects if running as a Windows Service and configures itself accordingly.  

- MongoDB Collection Auto-Creation:  
  If the Logs collection does not exist, it is automatically created as a Time-Series Collection.  
  Ensures correct granularity & expiration settings.  

- Failover Safety:  
  If the primary MongoDB instance is unreachable, the service gracefully switches to the defined secondary instance.  

## License

This project is licensed under the MIT License.

## Support & Contact

For assistance, contact SpecMetrix Support at:  
📧 greensboro.support@industrialphysics.com  
🌐 https://industrialphysics.com/brands/specmetrix/
