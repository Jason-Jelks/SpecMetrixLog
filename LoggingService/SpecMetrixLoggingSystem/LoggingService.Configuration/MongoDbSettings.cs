namespace LoggingService.Configuration
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = "mongodb://localhost:27017";
        public string DatabaseName { get; set; } = "Logging";
        public string CollectionName { get; set; } = "Logs";
        public string TimeField { get; set; } = "Timestamp";
        public string MetaField { get; set; } = "metadata";
        public string Granularity { get; set; } = "hours"; // Options: "seconds", "minutes", "hours"
        public int ExpireAfterDays { get; set; } = 7; // Default TTL: 7 Days
    }
}
