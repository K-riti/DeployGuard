using DeployGuard.Core.Policies;

namespace DeployGuard.Tests;

public class BranchPolicyCheckTests
{
    [Fact]
    public async Task EvaluateAsync_WhenNoAccessToken_ReturnsFail()
    {
        // Arrange
        Environment.SetEnvironmentVariable("SYSTEM_ACCESSTOKEN", null);
        var config = new BranchPolicyConfig
        {
            Enabled = true,
            TargetBranch = "main",
            Organization = "testorg",
            Project = "testproject"
        };
        var policy = new BranchPolicyCheck(config);

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
        var config = new BranchPolicyConfig
        {
            Enabled = true,
            TargetBranch = "main",
            Organization = "",
            Project = "testproject"
        };
        var policy = new BranchPolicyCheck(config);

        // Act
        var result = await policy.EvaluateAsync();

        // Assert
        Assert.False(result.Passed);
        Assert.Contains("Organization", result.Details);

        // Cleanup
        Environment.SetEnvironmentVariable("SYSTEM_ACCESSTOKEN", null);
    }

    [Fact]
    public void Name_ReturnsBranchPolicy()
    {
        // Arrange
        var config = new BranchPolicyConfig();
        var policy = new BranchPolicyCheck(config);

        // Act & Assert
        Assert.Equal("BranchPolicy", policy.Name);
    }

    [Fact]
    public void IsEnabled_WhenConfigEnabled_ReturnsTrue()
    {
        // Arrange
        var config = new BranchPolicyConfig { Enabled = true };
        var policy = new BranchPolicyCheck(config);

        // Act & Assert
        Assert.True(policy.IsEnabled);
    }

    [Fact]
    public void IsEnabled_WhenConfigDisabled_ReturnsFalse()
    {
        // Arrange
        var config = new BranchPolicyConfig { Enabled = false };
        var policy = new BranchPolicyCheck(config);

        // Act & Assert
        Assert.False(policy.IsEnabled);
    }
}
