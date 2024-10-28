using SpecMetrix.DataService;
using SpecMetrix.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Load the JSON configuration file
builder.Configuration.AddJsonFile(@"C:\Configurations\Specmetrix.json", optional: false, reloadOnChange: true);

// Register the LoggingService with its interface
builder.Services.AddSingleton<ILoggingService, LoggingService>();

// Register IDataService and other dependencies
builder.Services.AddScoped<IDataService, MongoDataService>();
builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.MapControllers(); // Enable API endpoints

app.Run();
