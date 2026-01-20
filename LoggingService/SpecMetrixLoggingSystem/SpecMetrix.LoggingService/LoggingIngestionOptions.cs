namespace SpecMetrix.LoggingService.Services
{
    /// <summary>
    /// Controls behavior of the logging ingestion pipeline.
    /// </summary>
    public sealed class LoggingIngestionOptions
    {
        /// <summary>
        /// Maximum number of log entries buffered in memory.
        /// </summary>
        public int Capacity { get; set; } = 10_000;

        /// <summary>
        /// When capacity is exceeded, drop the oldest entries instead of blocking.
        /// </summary>
        public bool DropOldest { get; set; } = true;

        /// <summary>
        /// Never drop critical events (e.g. config ingestion failures).
        /// </summary>
        public bool NeverDropCritical { get; set; } = true;

        /// <summary>
        /// EventId prefix considered critical.
        /// </summary>
        public string CriticalEventPrefix { get; set; } = "CFG_";
    }
}
