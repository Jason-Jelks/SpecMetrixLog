using Serilog;
using Serilog.Deduplication;
using Serilog.Filters;

var builder = WebApplication.CreateBuilder(args);

// Load the JSON configuration file from "C:\Configurations\Specmetrix.json"
builder.Configuration.AddJsonFile(@"C:\Configurations\Specmetrix.json", optional: false, reloadOnChange: true);

// Retrieve deduplication settings from the configuration
var deduplicationSettings = builder.Configuration.GetSection("Config:Logging:Deduplication").Get<DeduplicationSettings>();

// Configure Serilog with deduplication filter
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)           // Read Serilog settings from Specmetrix.json file
    .Filter.With(new DeduplicationFilter(deduplicationSettings)) // Add DeduplicationFilter to the pipeline
    .Enrich.FromLogContext()                                 // Add context information (e.g., thread, machine, etc.)
    .CreateLogger();

// Use Serilog as the logging provider for the application
builder.Host.UseSerilog();

// Register ILoggingService and its implementation, LoggingService, in the DI container
builder.Services.AddScoped<ILoggingService, LoggingService>();

// Register controllers and other services
builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.MapControllers(); // Enable API endpoints

try
{
    Log.Information("Starting application");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "The application failed to start.");
}
finally
{
    Log.CloseAndFlush(); // Ensure all buffered logs are flushed before shutdown
}
