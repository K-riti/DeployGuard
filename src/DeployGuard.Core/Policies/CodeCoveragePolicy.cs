using System.Text.Json;
using DeployGuard.Core.Engine;

namespace DeployGuard.Core.Policies;

public class CodeCoveragePolicy : AdoPolicyBase
{
    private readonly CodeCoverageConfig _config;

    public override string Name => "CodeCoverage";
    public override bool IsEnabled => _config.Enabled;

    public CodeCoveragePolicy(CodeCoverageConfig config, HttpClient? httpClient = null)
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

        var buildId = Environment.GetEnvironmentVariable("BUILD_BUILDID");
        if (string.IsNullOrEmpty(buildId))
            return PolicyResult.Fail(Name, "BUILD_BUILDID not available");

        var requestUrl = BuildAdoApiUrl(_config.Organization, _config.Project, $"test/codecoverage?buildId={buildId}&api-version=7.0");
        var request = CreateRequest(HttpMethod.Get, requestUrl);

        try
        {
            var response = await HttpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<CodeCoverageResponse>(content, JsonOptions);

            var lineCoverage = ExtractLineCoverage(result);

            return lineCoverage < _config.ThresholdPercent
                ? PolicyResult.Fail(Name, $"Code coverage {lineCoverage:F1}% is below threshold {_config.ThresholdPercent}%")
                : PolicyResult.Pass(Name, $"Code coverage {lineCoverage:F1}% meets threshold {_config.ThresholdPercent}%");
        }
        catch (Exception ex)
        {
            return PolicyResult.Fail(Name, $"Failed to retrieve code coverage: {ex.Message}");
        }
    }

    private static double ExtractLineCoverage(CodeCoverageResponse? response)
    {
        if (response?.CoverageData is null || response.CoverageData.Length == 0)
            return 0;

        var coverageData = response.CoverageData[0];
        var lineStats = coverageData.CoverageStats?.FirstOrDefault(s =>
            s.Label?.Equals("Lines", StringComparison.OrdinalIgnoreCase) == true ||
            s.Label?.Equals("Line", StringComparison.OrdinalIgnoreCase) == true);

        if (lineStats is null || lineStats.Total == 0)
            return 0;

        return (double)lineStats.Covered / lineStats.Total * 100;
    }

    private class CodeCoverageResponse
    {
        public CoverageData[]? CoverageData { get; set; }
    }

    private class CoverageData
    {
        public CoverageStat[]? CoverageStats { get; set; }
    }

    private class CoverageStat
    {
        public string? Label { get; set; }
        public int Total { get; set; }
        public int Covered { get; set; }
    }
}
