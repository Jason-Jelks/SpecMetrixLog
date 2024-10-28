using Microsoft.AspNetCore.Mvc;
using SpecMetrix.Shared.Logging;

[ApiController]
[Route("api/logs")]
public class LogController : ControllerBase
{
    private readonly LoggingService _loggingService;

    /// <summary>
    /// DI Log Controller/API for receiving log entries from services and applications
    /// </summary>
    /// <param name="loggingService"></param>
    public LogController(LoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    /// <summary>
    /// The API Entry point for receiving a log entry
    /// </summary>
    /// <param name="logEntry">Log data providing information about events/errors and the processes that generated them</param>
    /// <returns></returns>
    [HttpPost]
    public IActionResult ReceiveLog([FromBody] LogEntry logEntry)
    {
        // Enqueue the log entry for processing in the LoggingService
        _loggingService.EnqueueLog(logEntry);
        return Ok("Log received and processed.");
    }
}
