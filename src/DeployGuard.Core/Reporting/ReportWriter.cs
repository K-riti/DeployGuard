using System.Text.Json;
using DeployGuard.Core.Engine;

namespace DeployGuard.Core.Reporting;

public class ReportWriter
{
    private readonly string _outputDirectory;

    public ReportWriter(string? outputDirectory = null)
    {
        _outputDirectory = outputDirectory ?? Environment.GetEnvironmentVariable("BUILD_ARTIFACTSTAGINGDIRECTORY") ?? ".";
    }

    public async Task<string> WriteReportAsync(GateReport report)
    {
        var reportPath = Path.Combine(_outputDirectory, "gate-report.json");

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(report, options);
        await File.WriteAllTextAsync(reportPath, json);

        // Set ADO pipeline variables
        AdoPipelineLogger.SetVariable("GATE_RESULT", report.Approved ? "APPROVED" : "BLOCKED");
        AdoPipelineLogger.SetVariable("FAILED_POLICIES", string.Join(",", report.FailedPolicies));

        // Upload as pipeline artifact
        AdoPipelineLogger.UploadSummary(reportPath);

        return reportPath;
    }

    public void PrintSummary(GateReport report)
    {
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine($"  DEPLOYGUARD POLICY CHECK - {report.Environment.ToUpperInvariant()}");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();

        foreach (var result in report.PolicyResults)
        {
            if (result.Passed)
            {
                AdoPipelineLogger.LogSuccess($"{result.PolicyName}: {result.Details}");
            }
            else
            {
                AdoPipelineLogger.LogFailure($"{result.PolicyName}: {result.Details}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("───────────────────────────────────────────────────────────────");

        if (report.Approved)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  ✓ DEPLOYMENT APPROVED - All policies passed");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ DEPLOYMENT BLOCKED - Failed policies: {string.Join(", ", report.FailedPolicies)}");
            Console.ResetColor();
        }

        Console.WriteLine("───────────────────────────────────────────────────────────────");
        Console.WriteLine();
    }
}
