using DeployGuard.Core.Policies;

namespace DeployGuard.Tests;

public class CodeCoveragePolicyTests
{
    [Fact]
    public async Task EvaluateAsync_WhenNoAccessToken_ReturnsFail()
    {
        // Arrange
        Environment.SetEnvironmentVariable("SYSTEM_ACCESSTOKEN", null);
        var config = new CodeCoverageConfig
        {
            Enabled = true,
            ThresholdPercent = 80,
            Organization = "testorg",
            Project = "testproject"
        };
        var policy = new CodeCoveragePolicy(config);

        // Act
        var result = await policy.EvaluateAsync();

        // Assert
        Assert.False(result.Passed);
        Assert.Contains("SYSTEM_ACCESSTOKEN", result.Details);
    }

    [Fact]
    public async Task EvaluateAsync_WhenOrganizationNotConfigured_ReturnsFail()
    {
        // Arrange
        Environment.SetEnvironmentVariable("SYSTEM_ACCESSTOKEN", "test-token");
        var config = new CodeCoverageConfig
        {
            Enabled = true,
            ThresholdPercent = 80,
            Organization = "",
            Project = "testproject"
        };
        var policy = new CodeCoveragePolicy(config);

        // Act
        var result = await policy.EvaluateAsync();

        // Assert
        Assert.False(result.Passed);
        Assert.Contains("Organization", result.Details);

        // Cleanup
        Environment.SetEnvironmentVariable("SYSTEM_ACCESSTOKEN", null);
    }

    [Fact]
    public async Task EvaluateAsync_WhenBuildIdNotAvailable_ReturnsFail()
    {
        // Arrange
        Environment.SetEnvironmentVariable("SYSTEM_ACCESSTOKEN", "test-token");
        Environment.SetEnvironmentVariable("BUILD_BUILDID", null);
        var config = new CodeCoverageConfig
        {
            Enabled = true,
            ThresholdPercent = 80,
            Organization = "testorg",
            Project = "testproject"
        };
        var policy = new CodeCoveragePolicy(config);

        // Act
        var result = await policy.EvaluateAsync();

        // Assert
        Assert.False(result.Passed);
        Assert.Contains("BUILD_BUILDID", result.Details);

        // Cleanup
        Environment.SetEnvironmentVariable("SYSTEM_ACCESSTOKEN", null);
    }

    [Fact]
    public void Name_ReturnsCodeCoverage()
    {
        // Arrange
        var config = new CodeCoverageConfig();
        var policy = new CodeCoveragePolicy(config);

        // Act & Assert
        Assert.Equal("CodeCoverage", policy.Name);
    }

    [Fact]
    public void IsEnabled_WhenConfigEnabled_ReturnsTrue()
    {
        // Arrange
        var config = new CodeCoverageConfig { Enabled = true };
        var policy = new CodeCoveragePolicy(config);

        // Act & Assert
        Assert.True(policy.IsEnabled);
    }
}
