using Microsoft.AspNetCore.Mvc;
using SpecMetrix.Shared.Logging;

[ApiController]
[Route("api/logs")]
public class LogController : ControllerBase
{
    private readonly ILoggingService _loggingService;

    public LogController(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    [HttpPost]
    public IActionResult ReceiveLog([FromBody] LogEntry logEntry)
    {
        if (logEntry is null)
        {
            return BadRequest("Cannot have null log.");
        }

        _loggingService.EnqueueLog(logEntry);
        return Ok("Log received and processed.");
    }
}
