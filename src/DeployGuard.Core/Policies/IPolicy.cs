using DeployGuard.Core.Engine;

namespace DeployGuard.Core.Policies;

public interface IPolicy
{
    string Name { get; }
    bool IsEnabled { get; }
    Task<PolicyResult> EvaluateAsync(CancellationToken cancellationToken = default);
}
