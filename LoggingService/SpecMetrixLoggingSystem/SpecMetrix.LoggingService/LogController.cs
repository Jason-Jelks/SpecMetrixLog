using Microsoft.AspNetCore.Mvc;
using SpecMetrix.Shared.Logging;

[ApiController]
[Route("api/[controller]")]
public class LogController : ControllerBase
{
    private readonly ILoggingService _loggingService;

    public LogController(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

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
