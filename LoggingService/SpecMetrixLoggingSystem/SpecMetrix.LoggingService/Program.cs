
using SpecMetrix.DataService;
using SpecMetrix.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddHostedService<LoggingService>();
builder.Services.AddScoped<IDataService, MongoDataService>(); // Register your data service
builder.Services.AddControllers(); // Register controllers for handling incoming log events

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.MapControllers(); // Map the controller routes

app.Run();
