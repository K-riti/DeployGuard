namespace DeployGuard.Core.Reporting;

public static class AdoPipelineLogger
{
    public static void LogInfo(string message)
    {
        Console.WriteLine($"##[section]{message}");
    }

    public static void LogWarning(string message)
    {
        Console.WriteLine($"##[warning]{message}");
    }

    public static void LogError(string message)
    {
        Console.WriteLine($"##[error]{message}");
    }

    public static void LogSuccess(string message)
    {
        Console.WriteLine($"##[section]✓ {message}");
    }

    public static void LogFailure(string message)
    {
        Console.WriteLine($"##[error]✗ {message}");
    }

    public static void SetVariable(string name, string value, bool isSecret = false)
    {
        var secretFlag = isSecret ? ";issecret=true" : "";
        Console.WriteLine($"##vso[task.setvariable variable={name}{secretFlag}]{value}");
    }

    public static void UploadSummary(string filePath)
    {
        Console.WriteLine($"##vso[task.uploadsummary]{filePath}");
    }

    public static void UploadArtifact(string containerFolder, string artifactName, string filePath)
    {
        Console.WriteLine($"##vso[artifact.upload containerfolder={containerFolder};artifactname={artifactName}]{filePath}");
    }

    public static void SetTaskResult(bool success)
    {
        var result = success ? "Succeeded" : "Failed";
        Console.WriteLine($"##vso[task.complete result={result}]");
    }
}
