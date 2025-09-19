using LoggingService.Extensions.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SpecMetrix.Interfaces;
using SpecMetrix.LoggingService.Controllers;
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
            Namespace = "Unittest",
            MachineName = "TestMachine",
            Process = "UnitTestProcess",
            Message = "Test message",
            MessageTemplate = "Test message",
            RenderedMessage = "Test message",
            TemplateValues = new Dictionary<string, object>()
        };

        // Act
        var result = controller.ReceiveLog(logEntry);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Log received and processed.", okResult.Value);

        // Verify that EnqueueLog was called once
        mockLoggingService.Verify(s => s.EnqueueLog(It.IsAny<ILogEntry>()), Times.Once);
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

        // Verify EnqueueLog was never called
        mockLoggingService.Verify(s => s.EnqueueLog(It.IsAny<ILogEntry>()), Times.Never);
    }
}
