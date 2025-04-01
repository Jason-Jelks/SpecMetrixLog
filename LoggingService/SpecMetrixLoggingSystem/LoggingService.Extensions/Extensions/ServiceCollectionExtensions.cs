using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Deduplication;
using Serilog.Filters;
using LoggingService.Extensions.Interfaces;
using LoggingService.Extensions.Services;
using SpecMetrix.Interfaces;

namespace LoggingService.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the SpecMetrix logging pipeline to the DI container.
        /// Configures Serilog with deduplication and standard enrichers.
        /// </summary>
        public static IServiceCollection AddSpecMetrixLogging(this IServiceCollection services, IConfiguration configuration)
        {
            return services.AddSpecMetrixLogging(configuration, null, registerILoggingService: true);
        }

        /// <summary>
        /// Adds the SpecMetrix logging pipeline to the DI container with optional environment support.
        /// </summary>
        public static IServiceCollection AddSpecMetrixLogging(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment? environment,
            bool registerILoggingService = true)
        {
            // Register the Serilog abstraction
            services.AddSingleton<ISerilogWrapper, SerilogWrapperService>();

            // Register default logging service unless caller overrides it
            if (registerILoggingService)
            {
                services.AddScoped<ILoggingService, DefaultLoggingService>();
            }

            // Configure Serilog deduplication + sinks
            var deduplicationSettings = configuration
                .GetSection("Config:Logging:Deduplication")
                .Get<DeduplicationSettings>();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Filter.With(new DeduplicationFilter(deduplicationSettings))
                .Enrich.FromLogContext()
                .CreateLogger();

            if (environment != null)
            {
                Log.Information("Environment: {Env}", environment.EnvironmentName);
            }

            return services;
        }
    }
}
