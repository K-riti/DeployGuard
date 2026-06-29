using DeployGuard.Core.Policies;

namespace DeployGuard.Tests;

public class OpenP1BugsPolicyTests
{
    [Fact]
    public async Task EvaluateAsync_WhenNoAccessToken_ReturnsFail()
    {
        // Arrange
        Environment.SetEnvironmentVariable("SYSTEM_ACCESSTOKEN", null);
        var config = new OpenP1BugsConfig
        {
            Enabled = true,
            AreaPath = "TestProject\\Area",
            Organization = "testorg",
            Project = "testproject"
        };
        var policy = new OpenP1BugsPolicy(config);

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
        var config = new OpenP1BugsConfig
        {
            Enabled = true,
            AreaPath = "TestProject\\Area",
            Organization = "",
            Project = "testproject"
        };
        var policy = new OpenP1BugsPolicy(config);

        // Act
        var result = await policy.EvaluateAsync();

        // Assert
        Assert.False(result.Passed);
        Assert.Contains("Organization", result.Details);

        // Cleanup
        Environment.SetEnvironmentVariable("SYSTEM_ACCESSTOKEN", null);
    }

    [Fact]
    public void Name_ReturnsOpenP1Bugs()
    {
        // Arrange
        var config = new OpenP1BugsConfig();
        var policy = new OpenP1BugsPolicy(config);

        // Act & Assert
        Assert.Equal("OpenP1Bugs", policy.Name);
    }

    [Fact]
    public void IsEnabled_WhenConfigEnabled_ReturnsTrue()
    {
        // Arrange
        var config = new OpenP1BugsConfig { Enabled = true };
        var policy = new OpenP1BugsPolicy(config);

        // Act & Assert
        Assert.True(policy.IsEnabled);
    }
}
