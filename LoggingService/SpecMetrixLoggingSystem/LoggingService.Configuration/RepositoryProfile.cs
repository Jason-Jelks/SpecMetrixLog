namespace LoggingService.Configuration
{
    public class RepositoryProfile
    {
        public string Primary { get; set; } = string.Empty;
        public string? Secondary { get; set; }
        public string Mode { get; set; } = "PrimaryOnly"; // Options: PrimaryOnly, DualWrite, Failover
    }
}
