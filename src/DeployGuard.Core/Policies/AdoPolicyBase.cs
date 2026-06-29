using System.Net.Http.Headers;
using System.Text.Json;
using DeployGuard.Core.Engine;

namespace DeployGuard.Core.Policies;

/// <summary>
/// Base class for policies that interact with Azure DevOps REST APIs.
/// </summary>
public abstract class AdoPolicyBase : IPolicy
{
    protected readonly HttpClient HttpClient;
    protected static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public abstract string Name { get; }
    public abstract bool IsEnabled { get; }

    protected AdoPolicyBase(HttpClient? httpClient = null)
    {
        HttpClient = httpClient ?? new HttpClient();
    }

    public abstract Task<PolicyResult> EvaluateAsync(CancellationToken cancellationToken = default);

    protected static string? GetAccessToken() =>
        Environment.GetEnvironmentVariable("SYSTEM_ACCESSTOKEN");

    protected static PolicyResult? ValidateAccessToken(string policyName)
    {
        var token = GetAccessToken();
        return string.IsNullOrEmpty(token)
            ? PolicyResult.Fail(policyName, "SYSTEM_ACCESSTOKEN not available")
            : null;
    }

    protected static PolicyResult? ValidateOrgAndProject(string policyName, string? organization, string? project)
    {
        return string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(project)
            ? PolicyResult.Fail(policyName, "Organization or Project not configured")
            : null;
    }

    protected HttpRequestMessage CreateRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GetAccessToken());
        return request;
    }

    protected static string BuildAdoApiUrl(string organization, string project, string apiPath) =>
        $"https://dev.azure.com/{organization}/{project}/_apis/{apiPath}";
}
