using System.Text.Json;
using DeployGuard.Core.Engine;

namespace DeployGuard.Core.Policies;

public class BranchPolicyCheck : AdoPolicyBase
{
    private readonly BranchPolicyConfig _config;

    public override string Name => "BranchPolicy";
    public override bool IsEnabled => _config.Enabled;

    public BranchPolicyCheck(BranchPolicyConfig config, HttpClient? httpClient = null)
        : base(httpClient)
    {
        _config = config;
    }

    public override async Task<PolicyResult> EvaluateAsync(CancellationToken cancellationToken = default)
    {
        if (ValidateAccessToken(Name) is { } tokenError)
            return tokenError;

        if (ValidateOrgAndProject(Name, _config.Organization, _config.Project) is { } configError)
            return configError;

        var requestUrl = BuildAdoApiUrl(_config.Organization, _config.Project, "policy/configurations?api-version=7.0");
        var request = CreateRequest(HttpMethod.Get, requestUrl);

        try
        {
            var response = await HttpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<PolicyConfigResponse>(content, JsonOptions);

            var branchRef = $"refs/heads/{_config.TargetBranch}";
            var branchPolicies = result?.Value?.Where(p =>
                p.IsEnabled &&
                p.Settings?.Scope?.Any(s => s.RefName == branchRef) == true).ToList() ?? [];

            var issues = new List<string>();

            if (_config.RequireReviewers)
            {
                var hasReviewerPolicy = branchPolicies.Any(p =>
                    p.Type?.DisplayName?.Contains("Minimum number of reviewers", StringComparison.OrdinalIgnoreCase) == true ||
                    p.Type?.DisplayName?.Contains("Required reviewers", StringComparison.OrdinalIgnoreCase) == true);

                if (!hasReviewerPolicy)
                    issues.Add("Required reviewers policy not configured");
            }

            if (_config.RequireWorkItemLink)
            {
                var hasWorkItemPolicy = branchPolicies.Any(p =>
                    p.Type?.DisplayName?.Contains("Work item linking", StringComparison.OrdinalIgnoreCase) == true);

                if (!hasWorkItemPolicy)
                    issues.Add("Work item linking policy not configured");
            }

            return issues.Count > 0
                ? PolicyResult.Fail(Name, $"Branch policy issues on '{_config.TargetBranch}': {string.Join("; ", issues)}")
                : PolicyResult.Pass(Name, $"All required branch policies configured on '{_config.TargetBranch}'");
        }
        catch (Exception ex)
        {
            return PolicyResult.Fail(Name, $"Failed to check branch policies: {ex.Message}");
        }
    }

    private class PolicyConfigResponse
    {
        public PolicyConfiguration[]? Value { get; set; }
    }

    private class PolicyConfiguration
    {
        public bool IsEnabled { get; set; }
        public PolicyType? Type { get; set; }
        public PolicySettings? Settings { get; set; }
    }

    private class PolicyType
    {
        public string? DisplayName { get; set; }
    }

    private class PolicySettings
    {
        public PolicyScope[]? Scope { get; set; }
    }

    private class PolicyScope
    {
        public string? RefName { get; set; }
    }
}
