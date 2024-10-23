using SpecMetrix.DataService;
using SpecMetrix.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Load the JSON configuration file from "C:\Configurations\Specmetrix.json"
builder.Configuration.AddJsonFile(@"C:\Configurations\Specmetrix.json", optional: false, reloadOnChange: true);

// Add services to the container
builder.Services.AddHostedService<LoggingService>(); // Register LoggingService
builder.Services.AddScoped<IDataService, MongoDataService>(); // Register your data service
builder.Services.AddControllers(); // Register controllers for handling incoming log events

// Make the IConfiguration available for dependency injection in LoggingService
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.MapControllers(); // Map the controller routes

app.Run();
