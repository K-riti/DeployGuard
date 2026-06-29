namespace DeployGuard.Core.Engine;

public record PolicyResult(
    string PolicyName,
    bool Passed,
    string Details,
    DateTime CheckedAt
)
{
    public static PolicyResult Pass(string policyName, string details) =>
        new(policyName, true, details, DateTime.UtcNow);

    public static PolicyResult Fail(string policyName, string details) =>
        new(policyName, false, details, DateTime.UtcNow);
}
