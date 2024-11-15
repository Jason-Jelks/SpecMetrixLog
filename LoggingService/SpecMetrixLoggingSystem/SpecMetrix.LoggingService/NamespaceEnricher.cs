using Serilog.Core;
using Serilog.Events;

public class NamespaceEnricher : ILogEventEnricher
{
    private readonly string _namespace;

    public NamespaceEnricher(string namespaceName)
    {
        _namespace = namespaceName;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("Namespace", _namespace));
    }
}
