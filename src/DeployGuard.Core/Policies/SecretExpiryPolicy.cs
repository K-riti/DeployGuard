using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using DeployGuard.Core.Engine;

namespace DeployGuard.Core.Policies;

public class SecretExpiryPolicy : IPolicy
{
    private readonly SecretExpiryConfig _config;
    private readonly SecretClient? _secretClient;

    public string Name => "SecretExpiry";
    public bool IsEnabled => _config.Enabled;

    public SecretExpiryPolicy(SecretExpiryConfig config, SecretClient? secretClient = null)
    {
        _config = config;
        _secretClient = secretClient ?? (string.IsNullOrEmpty(config.KeyVaultName) ? null
            : new SecretClient(
                new Uri($"https://{config.KeyVaultName}.vault.azure.net/"),
                new DefaultAzureCredential()));
    }

    public async Task<PolicyResult> EvaluateAsync(CancellationToken cancellationToken = default)
    {
        if (_secretClient is null)
            return PolicyResult.Fail(Name, "KeyVault name not configured");

        var expiringSecrets = new List<string>();
        var warningDate = DateTime.UtcNow.AddDays(_config.WarningDays);

        await foreach (var secretProperties in _secretClient.GetPropertiesOfSecretsAsync(cancellationToken))
        {
            if (secretProperties.ExpiresOn.HasValue &&
                secretProperties.ExpiresOn.Value <= warningDate)
            {
                expiringSecrets.Add($"{secretProperties.Name} (expires: {secretProperties.ExpiresOn.Value:yyyy-MM-dd})");
            }
        }

        if (expiringSecrets.Count > 0)
        {
            return PolicyResult.Fail(Name,
                $"Secrets expiring within {_config.WarningDays} days: {string.Join(", ", expiringSecrets)}");
        }

        return PolicyResult.Pass(Name, $"No secrets expiring within {_config.WarningDays} days");
    }
}
