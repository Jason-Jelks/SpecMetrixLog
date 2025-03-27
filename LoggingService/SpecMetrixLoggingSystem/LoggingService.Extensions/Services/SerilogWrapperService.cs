using SpecMetrix.Interfaces;
using LoggingService.Extensions.Interfaces;

namespace LoggingService.Extensions.Services
{

    public class SerilogWrapperService : ISerilogWrapper
    {
        public void Log(ILogEntry logEntry)
        {
            string message = RenderMessageTemplate(logEntry.MessageTemplate, logEntry.TemplateValues, logEntry.Message);

            var logger = Serilog.Log.ForContext("Namespace", logEntry.Namespace ?? string.Empty)
                            .ForContext("Code", logEntry.Code)
                            .ForContext("Process", logEntry.Process)
                            .ForContext("Category", logEntry.Category)
                            .ForContext("Source", logEntry.Source)
                            .ForContext("DeviceName", logEntry.DeviceName)
                            .ForContext("MachineName", logEntry.MachineName ?? string.Empty)
                            .ForContext("ClassMethod", logEntry.ClassMethod)
                            .ForContext("TemplateValues", logEntry.TemplateValues);

            switch (logEntry.Level)
            {
                case LogLevel.Critical: logger.Fatal(message); break;
                case LogLevel.Error: logger.Error(message); break;
                case LogLevel.Warning: logger.Warning(message); break;
                case LogLevel.Debug: logger.Debug(message); break;
                case LogLevel.Trace: logger.Verbose(message); break;
                default: logger.Information(message); break;
            }
        }

        private string RenderMessageTemplate(string template, IDictionary<string, object> values, string originalMessage)
        {
            if (string.IsNullOrWhiteSpace(template))
                return originalMessage;

            foreach (var value in values)
                template = template.Replace("{" + value.Key + "}", value.Value?.ToString() ?? string.Empty);

            return template;
        }
    }
}
