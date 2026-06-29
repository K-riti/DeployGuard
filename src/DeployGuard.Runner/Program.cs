using DeployGuard.Core.Engine;
using DeployGuard.Core.Policies;
using DeployGuard.Core.Reporting;
using Microsoft.Extensions.Configuration;

namespace DeployGuard.Runner;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var config = LoadConfiguration(args);
            var policyConfig = new PolicyConfig();
            config.Bind(policyConfig);

            var environment = config["environment"] ?? "unknown";

            // Override from command line arguments
            ApplyCommandLineOverrides(args, policyConfig);

            var runner = new PolicyRunner()
                .AddPolicy(new SecretExpiryPolicy(policyConfig.SecretExpiry))
                .AddPolicy(new OpenP1BugsPolicy(policyConfig.OpenP1Bugs))
                .AddPolicy(new CodeCoveragePolicy(policyConfig.CodeCoverage))
                .AddPolicy(new BranchPolicyCheck(policyConfig.BranchPolicy));

            AdoPipelineLogger.LogInfo($"Starting DeployGuard policy check for environment: {environment}");

            var report = await runner.RunAsync(environment);

            var reportWriter = new ReportWriter();
            reportWriter.PrintSummary(report);
            await reportWriter.WriteReportAsync(report);

            AdoPipelineLogger.SetTaskResult(report.Approved);

            return report.Approved ? 0 : 1;
        }
        catch (Exception ex)
        {
            AdoPipelineLogger.LogError($"DeployGuard failed with error: {ex.Message}");
            return 1;
        }
    }

    private static IConfiguration LoadConfiguration(string[] args)
    {
        var configPath = args.FirstOrDefault(a => a.StartsWith("--config="))?.Split('=')[1]
            ?? "deployguard.json";

        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(configPath, optional: true, reloadOnChange: false)
            .AddEnvironmentVariables("DEPLOYGUARD_");

        return builder.Build();
    }

    private static void ApplyCommandLineOverrides(string[] args, PolicyConfig config)
    {
        foreach (var arg in args)
        {
            var parts = arg.TrimStart('-').Split('=', 2);
            if (parts.Length != 2) continue;

            var key = parts[0].ToLowerInvariant();
            var value = parts[1];

            switch (key)
            {
                case "keyvaultname":
                    config.SecretExpiry.KeyVaultName = value;
                    break;
                case "areapath":
                    config.OpenP1Bugs.AreaPath = value;
                    break;
                case "coveragethreshold":
                    if (int.TryParse(value, out var threshold))
                        config.CodeCoverage.ThresholdPercent = threshold;
                    break;
                case "secretexpirywarningdays":
                    if (int.TryParse(value, out var days))
                        config.SecretExpiry.WarningDays = days;
                    break;
                case "organization":
                    config.OpenP1Bugs.Organization = value;
                    config.CodeCoverage.Organization = value;
                    config.BranchPolicy.Organization = value;
                    break;
                case "project":
                    config.OpenP1Bugs.Project = value;
                    config.CodeCoverage.Project = value;
                    config.BranchPolicy.Project = value;
                    break;
                case "targetbranch":
                    config.BranchPolicy.TargetBranch = value;
                    break;
            }
        }
    }
}
