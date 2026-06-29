using DeployGuard.Core.Engine;
using DeployGuard.Core.Policies;
using Moq;

namespace DeployGuard.Tests;

public class PolicyRunnerTests
{
    [Fact]
    public async Task RunAsync_WithNoEnabledPolicies_ReturnsEmptyReport()
    {
        // Arrange
        var runner = new PolicyRunner();

        // Act
        var report = await runner.RunAsync("test");

        // Assert
        Assert.Empty(report.PolicyResults);
        Assert.True(report.Approved);
        Assert.Equal("test", report.Environment);
    }

    [Fact]
    public async Task RunAsync_WithPassingPolicy_ReturnsApprovedReport()
    {
        // Arrange
        var mockPolicy = new Mock<IPolicy>();
        mockPolicy.Setup(p => p.Name).Returns("TestPolicy");
        mockPolicy.Setup(p => p.IsEnabled).Returns(true);
        mockPolicy.Setup(p => p.EvaluateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(PolicyResult.Pass("TestPolicy", "All good"));

        var runner = new PolicyRunner()
            .AddPolicy(mockPolicy.Object);

        // Act
        var report = await runner.RunAsync("production");

        // Assert
        Assert.Single(report.PolicyResults);
        Assert.True(report.Approved);
        Assert.Empty(report.FailedPolicies);
    }

    [Fact]
    public async Task RunAsync_WithFailingPolicy_ReturnsBlockedReport()
    {
        // Arrange
        var mockPolicy = new Mock<IPolicy>();
        mockPolicy.Setup(p => p.Name).Returns("FailingPolicy");
        mockPolicy.Setup(p => p.IsEnabled).Returns(true);
        mockPolicy.Setup(p => p.EvaluateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(PolicyResult.Fail("FailingPolicy", "Something went wrong"));

        var runner = new PolicyRunner()
            .AddPolicy(mockPolicy.Object);

        // Act
        var report = await runner.RunAsync("production");

        // Assert
        Assert.Single(report.PolicyResults);
        Assert.False(report.Approved);
        Assert.Contains("FailingPolicy", report.FailedPolicies);
    }

    [Fact]
    public async Task RunAsync_WithMixedPolicies_ReturnsBlockedWhenAnyFails()
    {
        // Arrange
        var passingPolicy = new Mock<IPolicy>();
        passingPolicy.Setup(p => p.Name).Returns("PassingPolicy");
        passingPolicy.Setup(p => p.IsEnabled).Returns(true);
        passingPolicy.Setup(p => p.EvaluateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(PolicyResult.Pass("PassingPolicy", "OK"));

        var failingPolicy = new Mock<IPolicy>();
        failingPolicy.Setup(p => p.Name).Returns("FailingPolicy");
        failingPolicy.Setup(p => p.IsEnabled).Returns(true);
        failingPolicy.Setup(p => p.EvaluateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(PolicyResult.Fail("FailingPolicy", "Failed"));

        var runner = new PolicyRunner()
            .AddPolicy(passingPolicy.Object)
            .AddPolicy(failingPolicy.Object);

        // Act
        var report = await runner.RunAsync("production");

        // Assert
        Assert.Equal(2, report.PolicyResults.Count);
        Assert.False(report.Approved);
        Assert.Single(report.FailedPolicies);
        Assert.Contains("FailingPolicy", report.FailedPolicies);
    }

    [Fact]
    public async Task RunAsync_WithDisabledPolicy_SkipsPolicy()
    {
        // Arrange
        var disabledPolicy = new Mock<IPolicy>();
        disabledPolicy.Setup(p => p.Name).Returns("DisabledPolicy");
        disabledPolicy.Setup(p => p.IsEnabled).Returns(false);

        var runner = new PolicyRunner()
            .AddPolicy(disabledPolicy.Object);

        // Act
        var report = await runner.RunAsync("production");

        // Assert
        Assert.Empty(report.PolicyResults);
        Assert.True(report.Approved);
    }

    [Fact]
    public async Task RunAsync_WhenPolicyThrowsException_ReturnsFail()
    {
        // Arrange
        var throwingPolicy = new Mock<IPolicy>();
        throwingPolicy.Setup(p => p.Name).Returns("ThrowingPolicy");
        throwingPolicy.Setup(p => p.IsEnabled).Returns(true);
        throwingPolicy.Setup(p => p.EvaluateAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        var runner = new PolicyRunner()
            .AddPolicy(throwingPolicy.Object);

        // Act
        var report = await runner.RunAsync("production");

        // Assert
        Assert.Single(report.PolicyResults);
        Assert.False(report.Approved);
        Assert.Contains("Unexpected error", report.PolicyResults[0].Details);
    }
}
