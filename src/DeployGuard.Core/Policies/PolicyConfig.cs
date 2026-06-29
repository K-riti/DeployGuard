namespace DeployGuard.Core.Policies;

public class PolicyConfig
{
    public SecretExpiryConfig SecretExpiry { get; set; } = new();
    public OpenP1BugsConfig OpenP1Bugs { get; set; } = new();
    public CodeCoverageConfig CodeCoverage { get; set; } = new();
    public BranchPolicyConfig BranchPolicy { get; set; } = new();
}

public class SecretExpiryConfig
{
    public bool Enabled { get; set; } = true;
    public int WarningDays { get; set; } = 14;
    public string KeyVaultName { get; set; } = string.Empty;
}

public class OpenP1BugsConfig
{
    public bool Enabled { get; set; } = true;
    public string AreaPath { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;
    public string Project { get; set; } = string.Empty;
}

public class CodeCoverageConfig
{
    public bool Enabled { get; set; } = true;
    public int ThresholdPercent { get; set; } = 80;
    public string Organization { get; set; } = string.Empty;
    public string Project { get; set; } = string.Empty;
}

public class BranchPolicyConfig
{
    public bool Enabled { get; set; } = true;
    public string TargetBranch { get; set; } = "main";
    public bool RequireReviewers { get; set; } = true;
    public bool RequireWorkItemLink { get; set; } = true;
    public string Organization { get; set; } = string.Empty;
    public string Project { get; set; } = string.Empty;
    public string RepositoryId { get; set; } = string.Empty;
}
