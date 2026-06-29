namespace DeployGuard.Core.Engine;

public class GateReport
{
    public string Environment { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public bool Approved => PolicyResults.All(r => r.Passed);
    public List<PolicyResult> PolicyResults { get; set; } = [];
    public List<string> FailedPolicies => PolicyResults
        .Where(r => !r.Passed)
        .Select(r => r.PolicyName)
        .ToList();
}
