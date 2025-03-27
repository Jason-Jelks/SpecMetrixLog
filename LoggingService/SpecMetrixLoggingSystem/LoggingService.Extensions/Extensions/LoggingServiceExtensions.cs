using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Deduplication;
using LoggingService.Extensions.Interfaces;
using LoggingService.Extensions.Services;

namespace LoggingService.Extensions
{

    public static class LoggingServiceExtensions
    {
        public static IServiceCollection AddSpecMetrixLogging(this IServiceCollection services, IConfiguration configuration)
        {
            var dedupSettings = configuration.GetSection("Config:Logging:Deduplication").Get<DeduplicationSettings>();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Filter.With(new DeduplicationFilter(dedupSettings))
                .Enrich.FromLogContext()
                .CreateLogger();

            services.AddSingleton<ISerilogWrapper, SerilogWrapperService>();
            return services;
        }
    }
}
