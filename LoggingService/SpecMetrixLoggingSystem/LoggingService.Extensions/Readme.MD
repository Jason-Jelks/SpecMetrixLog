# 📦 SpecMetrix Logging Integration Guide

This guide explains how to integrate centralized structured logging using the `LoggingService.Extensions` NuGet package and the `LoggingServiceSetup.msi` installer.

---

## ✅ Required Setup Steps

### 1. Install the Logging Service

Run the `LoggingServiceSetup.msi` to install and register the SpecMetrix Logging Windows Service.

- This installs the logging API listener and background MongoDB writer.
- It reads config from `C:\Configurations\Specmetrix.json`.

> If you’re deploying to Linux or container-based environments, contact the team for the appropriate deployment strategy.

---

### 2. Install Required NuGet Packages

In your project:

```bash
dotnet add package LoggingService.Extensions
dotnet add package Serilog
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.MongoDB
dotnet add package Serilog.Deduplication
```

---

## 🛠 Required Configuration

Your `specmetrix.json` must be placed at:

```file
C:\Configurations\Specmetrix.json
```


And should include at least the following sections:

### ✅ Minimal Working Logging Configuration (with optional failover):

```json
{
  "Serilog": {
    "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.MongoDB" ],
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Warning",
        "LogSender": "Debug",
        "LogSender.*": "Debug"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "MongoDB",
        "Args": {
          "databaseUrl": "mongodb://localhost:27017/Logging",
          "collectionName": "Logs"
        }
      }
    ]
  },
  "Config": {
    "Logging": {
      "Deduplication": {
        "Error": {
          "DeduplicationEnabled": true,
          "DeduplicationWindowMilliseconds": 5000
        },
        "Warning": {
          "DeduplicationEnabled": true,
          "DeduplicationWindowMilliseconds": 3000
        },
        "Information": {
          "DeduplicationEnabled": true,
          "DeduplicationWindowMilliseconds": 1000
        },
        "Debug": {
          "DeduplicationEnabled": true,
          "DeduplicationWindowMilliseconds": 1000
        },
        "Verbose": {
          "DeduplicationEnabled": false,
          "DeduplicationWindowMilliseconds": 1000
        },
        "PruneIntervalMilliseconds": 60000,
        "CacheExpirationMilliseconds": 300000,
        "KeyProperties": [ "Code", "Source" ],
        "IncludeMessageTemplate": true
      }
    }
  },
  "Databases": {
    "LoggingMongoPrimary": {
      "Type": "MongoDb",
      "ConnectionString": "mongodb://localhost:27017",
      "DatabaseName": "Logging",
      "CollectionName": "Logs",
      "TimeField": "Timestamp",
      "MetaField": "metadata",
      "Granularity": "minutes",
      "ExpireAfterDays": 14
    },
    "LoggingMongoSecondary": {
      "Type": "MongoDb",
      "ConnectionString": "mongodb://backuphost:27017",
      "DatabaseName": "Logging",
      "CollectionName": "Logs",
      "TimeField": "Timestamp",
      "MetaField": "metadata",
      "Granularity": "minutes",
      "ExpireAfterDays": 14
    }
  },
  "LoggingRepositoryProfile": {
    "Primary": "LoggingMongoPrimary",
    "Secondary": "LoggingMongoSecondary", // Optional
    "Mode": "Failover" // Options: PrimaryOnly, Failover, DualWrite
  }
}
```

> `Secondary` and `Mode: Failover` are optional. If omitted, only the `Primary` logging target will be used.

---

## 🧩 Program.cs Integration

In your project’s `Program.cs`, register the logging:

```csharp
using LoggingService.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Register Serilog and structured logging pipeline
builder.Services.AddSpecMetrixLogging(builder.Configuration);
builder.Host.UseSerilog();
```

---

## 💉 Using the Logger in Your Service

Inject `ILoggingService` into your class, then call `EnqueueLog(...)` with structured values:

```csharp
using LoggingService.Extensions.Interfaces;
using SpecMetrix.Interfaces;

public class UserService
{
    private readonly ILoggingService _logger;

    public UserService(ILoggingService logger)
    {
        _logger = logger;
    }

    public void ProcessUser(int userId)
    {
        _logger.EnqueueLog(new LogEntry
        {
            MessageTemplate = "Processed user {UserId}",
            TemplateValues = new Dictionary<string, object>
            {
                { "UserId", userId }
            },
            Level = LogLevel.Information,
            Namespace = "UserService",
            Source = "API",
            Code = "USR2001"
        });
    }
}
```

This will store structured log data in MongoDB, including metadata fields like `Code`, `Source`, `Namespace`, and others.

---

## 🚫 Do NOT Use `Log.Information(...)` Directly

Use the injected `ILoggingService` interface so that:
- Logs are routed through deduplication and metadata enrichment
- Your service stays decoupled from Serilog
- Future routing (e.g., to different sinks or log processors) remains centralized

---

## 🧪 Testing & Troubleshooting

- Verify the logging service is running as a Windows Service
- Inspect `Logs` collection in MongoDB (`Logging` database)
- Check Serilog output to Console for startup messages
- Confirm `specmetrix.json` is correctly located in `C:\Configurations`

---

## ✅ Summary

| Step                        | What You Do                                     |
|-----------------------------|--------------------------------------------------|
| Install service             | Run `LoggingServiceSetup.msi`                   |
| Install NuGet packages      | Add `LoggingService.Extensions` and Serilog     |
| Create `Specmetrix.json`    | Place in `C:\Configurations\Specmetrix.json`    |
| Register in Program.cs      | Call `AddSpecMetrixLogging(...)`                |
| Use logging                 | Inject `ILoggingService` and call `EnqueueLog()`|

---

Let the team know they can reach out if they need help integrating with non-.NET apps or want to stream logs into a centralized UI/dashboard.
