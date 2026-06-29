using System.Text;
using System.Text.Json;
using DeployGuard.Core.Engine;

namespace DeployGuard.Core.Policies;

public class OpenP1BugsPolicy : AdoPolicyBase
{
    private readonly OpenP1BugsConfig _config;

    public override string Name => "OpenP1Bugs";
    public override bool IsEnabled => _config.Enabled;

    public OpenP1BugsPolicy(OpenP1BugsConfig config, HttpClient? httpClient = null)
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

        var wiqlQuery = new
        {
            query = $@"
                SELECT [System.Id], [System.Title]
                FROM WorkItems
                WHERE [System.WorkItemType] = 'Bug'
                  AND [Microsoft.VSTS.Common.Priority] = 1
                  AND [System.State] = 'Active'
                  AND [System.AreaPath] UNDER '{_config.AreaPath}'"
        };

        var requestUrl = BuildAdoApiUrl(_config.Organization, _config.Project, "wit/wiql?api-version=7.0");
        var request = CreateRequest(HttpMethod.Post, requestUrl);
        request.Content = new StringContent(JsonSerializer.Serialize(wiqlQuery), Encoding.UTF8, "application/json");

        try
        {
            var response = await HttpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<WiqlResponse>(content, JsonOptions);

            var bugCount = result?.WorkItems?.Length ?? 0;

            return bugCount > 0
                ? PolicyResult.Fail(Name, $"Found {bugCount} active P1 bug(s) in area path '{_config.AreaPath}'")
                : PolicyResult.Pass(Name, $"No active P1 bugs found in area path '{_config.AreaPath}'");
        }
        catch (Exception ex)
        {
            return PolicyResult.Fail(Name, $"Failed to query work items: {ex.Message}");
        }
    }

    private class WiqlResponse
    {
        public WorkItemReference[]? WorkItems { get; set; }
    }

    private class WorkItemReference
    {
        public int Id { get; set; }
        public string? Url { get; set; }
    }
}
