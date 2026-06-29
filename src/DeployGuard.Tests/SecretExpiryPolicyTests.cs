using DeployGuard.Core.Engine;
using DeployGuard.Core.Policies;
using Moq;

namespace DeployGuard.Tests;

public class SecretExpiryPolicyTests
{
    [Fact]
    public async Task EvaluateAsync_WhenKeyVaultNotConfigured_ReturnsFail()
    {
        // Arrange
        var config = new SecretExpiryConfig
        {
            Enabled = true,
            KeyVaultName = "",
            WarningDays = 14
        };
        var policy = new SecretExpiryPolicy(config);

        // Act
        var result = await policy.EvaluateAsync();

        // Assert
        Assert.False(result.Passed);
        Assert.Contains("not configured", result.Details);
    }

    [Fact]
    public void Name_ReturnsSecretExpiry()
    {
        // Arrange
        var config = new SecretExpiryConfig();
        var policy = new SecretExpiryPolicy(config);

        // Act & Assert
        Assert.Equal("SecretExpiry", policy.Name);
    }

    [Fact]
    public void IsEnabled_WhenConfigEnabled_ReturnsTrue()
    {
        // Arrange
        var config = new SecretExpiryConfig { Enabled = true };
        var policy = new SecretExpiryPolicy(config);

        // Act & Assert
        Assert.True(policy.IsEnabled);
    }

    [Fact]
    public void IsEnabled_WhenConfigDisabled_ReturnsFalse()
    {
        // Arrange
        var config = new SecretExpiryConfig { Enabled = false };
        var policy = new SecretExpiryPolicy(config);

        // Act & Assert
        Assert.False(policy.IsEnabled);
    }
}
