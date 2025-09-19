using LoggingService.Extensions.Interfaces;
using Microsoft.AspNetCore.Mvc;
using SpecMetrix.Shared.Logging;          // LogEntry

namespace SpecMetrix.LoggingService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]            // /api/log
    public sealed class LogController : ControllerBase
    {
        private readonly ILoggingService _logging;

        public LogController(ILoggingService logging)
        {
            _logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        /// <summary>
        /// Accept a single log entry and enqueue it for processing.
        /// NOTE: Production contract is singular controller: /api/log
        /// </summary>
        [HttpPost]
        public IActionResult ReceiveLog([FromBody] LogEntry entry)
        {
            if (entry == null)
                return BadRequest("Cannot have null log.");

            // If SpecMetrix.Shared.Logging.LogEntry implements SpecMetrix.Interfaces.ILogEntry,
            // this cast is a no-op; otherwise, adapt as needed.
            _logging.EnqueueLog(entry);

            // Keep response simple & fast for callers; they don't need to wait on storage.
            return Ok("Log received and processed.");
        }
    }
}
