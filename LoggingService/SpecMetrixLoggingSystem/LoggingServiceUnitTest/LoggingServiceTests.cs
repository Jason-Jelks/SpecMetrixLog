
using LoggingService.Extensions.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SpecMetrix.Shared.Logging;

public class LogControllerTests
{
    [Fact]
    public void ReceiveLog_ValidLogEntry_ReturnsOk()
    {
        // Arrange
        var mockLoggingService = new Mock<ILoggingService>();
        var controller = new LogController(mockLoggingService.Object);
        var logEntry = new LogEntry
        {
            // Populate required properties of LogEntry
            Namespace = "Unittest",
            MachineName = "Test",
            Process = "Testing",
            Message = "",
            MessageTemplate = "",
            RenderedMessage = "",
            TemplateValues = new Dictionary<string, object>(),
        };

        // Act
        var result = controller.ReceiveLog(logEntry);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Log received and processed.", okResult.Value);
    }

    [Fact]
    public void ReceiveLog_NullLogEntry_ReturnsBadRequest()
    {
        // Arrange
        var mockLoggingService = new Mock<ILoggingService>();
        var controller = new LogController(mockLoggingService.Object);

        // Act
        var result = controller.ReceiveLog(null);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Cannot have null log.", badRequestResult.Value);
    }
}
