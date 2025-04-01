using SpecMetrix.Interfaces;
using LoggingService.Extensions.Interfaces;

namespace LoggingService.Extensions.Services
{

    public class SerilogWrapperService : ISerilogWrapper
    {
        public void Log(ILogEntry logEntry)
        {
            var logger = Serilog.Log.ForContext("Namespace", logEntry.Namespace ?? string.Empty)
                                    .ForContext("Code", logEntry.Code)
                                    .ForContext("Process", logEntry.Process)
                                    .ForContext("Category", logEntry.Category)
                                    .ForContext("Source", logEntry.Source)
                                    .ForContext("DeviceName", logEntry.DeviceName)
                                    .ForContext("MachineName", logEntry.MachineName ?? string.Empty)
                                    .ForContext("ClassMethod", logEntry.ClassMethod)
                                    .ForContext("TemplateValues", logEntry.TemplateValues);

            bool hasStructuredValues = logEntry.TemplateValues != null && logEntry.TemplateValues.Count > 0;

            string renderedMessage = RenderMessageTemplate(logEntry.MessageTemplate, logEntry.TemplateValues, logEntry.Message);
            string template = logEntry.MessageTemplate ?? renderedMessage;

            object[] values = hasStructuredValues
                ? logEntry.TemplateValues.Values.ToArray()
                : Array.Empty<object>();

            switch (logEntry.Level)
            {
                case LogLevel.Critical:
                    if (hasStructuredValues) logger.Fatal(template, values); else logger.Fatal(renderedMessage);
                    break;
                case LogLevel.Error:
                    if (hasStructuredValues) logger.Error(template, values); else logger.Error(renderedMessage);
                    break;
                case LogLevel.Warning:
                    if (hasStructuredValues) logger.Warning(template, values); else logger.Warning(renderedMessage);
                    break;
                case LogLevel.Debug:
                    if (hasStructuredValues) logger.Debug(template, values); else logger.Debug(renderedMessage);
                    break;
                case LogLevel.Trace:
                    if (hasStructuredValues) logger.Verbose(template, values); else logger.Verbose(renderedMessage);
                    break;
                default:
                    if (hasStructuredValues) logger.Information(template, values); else logger.Information(renderedMessage);
                    break;
            }
        }


        private string RenderMessageTemplate(string template, IDictionary<string, object>? values, string originalMessage)
        {
            if (string.IsNullOrWhiteSpace(template) || values == null || values.Count == 0)
                return originalMessage;

            foreach (var value in values)
                template = template.Replace("{" + value.Key + "}", value.Value?.ToString() ?? string.Empty);

            return template;
        }
    }
}
