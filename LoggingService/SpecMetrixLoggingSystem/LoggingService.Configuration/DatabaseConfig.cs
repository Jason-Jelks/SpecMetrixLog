namespace LoggingService.Configuration
{
    public class DatabaseConfig
    {
        public string Type { get; set; } = "MongoDb";
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string CollectionName { get; set; } = "Logs";
        public string TimeField { get; set; } = "Timestamp";
        public string MetaField { get; set; } = "metadata";
        public string Granularity { get; set; } = "hours"; // Options: "seconds", "minutes", "hours"
        public int ExpireAfterDays { get; set; } = 7; // Default TTL: 7 Days
    }
}
