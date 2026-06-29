using DeployGuard.Core.Policies;

namespace DeployGuard.Core.Engine;

public class PolicyRunner
{
    private readonly List<IPolicy> _policies = [];

    public PolicyRunner AddPolicy(IPolicy policy)
    {
        if (policy.IsEnabled)
            _policies.Add(policy);
        return this;
    }

    public async Task<GateReport> RunAsync(string environment, CancellationToken cancellationToken = default)
    {
        var report = new GateReport
        {
            Environment = environment,
            GeneratedAt = DateTime.UtcNow
        };

        if (_policies.Count == 0)
        {
            return report;
        }

        // Run all policies in parallel
        var tasks = _policies.Select(p => RunPolicyAsync(p, cancellationToken));
        var results = await Task.WhenAll(tasks);

        report.PolicyResults = results.ToList();
        return report;
    }

    private static async Task<PolicyResult> RunPolicyAsync(IPolicy policy, CancellationToken cancellationToken)
    {
        try
        {
            return await policy.EvaluateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            return PolicyResult.Fail(policy.Name, $"Policy execution failed: {ex.Message}");
        }
    }
}
