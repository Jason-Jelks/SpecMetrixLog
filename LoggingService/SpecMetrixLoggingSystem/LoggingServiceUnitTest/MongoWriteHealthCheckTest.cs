using LoggingService.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;

namespace LoggingServiceUnitTest.Health
{
    public class MongoWriteHealthCheckTests
    {
        [Fact]
        public async Task CheckHealthAsync_WhenVerifierReturnsOk_Healthy()
        {
            // Arrange
            var mockVerifier = new Mock<IMongoWriteVerifier>();
            mockVerifier
                .Setup(v => v.VerifyWriteAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((true, string.Empty));

            var hc = new MongoWriteHealthCheck(mockVerifier.Object);
            var ctx = new HealthCheckContext();

            // Act
            var result = await hc.CheckHealthAsync(ctx, CancellationToken.None);

            // Assert
            Assert.Equal(HealthStatus.Healthy, result.Status);
            Assert.Equal("MongoDB write path is healthy.", result.Description);
        }

        [Fact]
        public async Task CheckHealthAsync_WhenVerifierFails_UnhealthyWithError()
        {
            // Arrange
            var mockVerifier = new Mock<IMongoWriteVerifier>();
            mockVerifier
                .Setup(v => v.VerifyWriteAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((false, "simulated failure"));

            var hc = new MongoWriteHealthCheck(mockVerifier.Object);
            var ctx = new HealthCheckContext();

            // Act
            var result = await hc.CheckHealthAsync(ctx, CancellationToken.None);

            // Assert
            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Equal("MongoDB write path failed.", result.Description);
            Assert.True(result.Data.ContainsKey("error"));
            Assert.Equal("simulated failure", result.Data["error"] as string);
        }

        [Fact]
        public async Task CheckHealthAsync_WhenVerifierFailsWithEmptyMessage_UnhealthyUnknown()
        {
            // Arrange
            var mockVerifier = new Mock<IMongoWriteVerifier>();
            mockVerifier
                .Setup(v => v.VerifyWriteAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((false, string.Empty));

            var hc = new MongoWriteHealthCheck(mockVerifier.Object);
            var ctx = new HealthCheckContext();

            // Act
            var result = await hc.CheckHealthAsync(ctx, CancellationToken.None);

            // Assert
            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Equal("MongoDB write path failed.", result.Description);
            Assert.True(result.Data.ContainsKey("error"));
            Assert.Equal("unknown", result.Data["error"] as string);
        }
    }
}
