namespace MTE.Core.Models;

public sealed class VerificationResult
{
    public string SourcePath { get; init; } = string.Empty;

    public string DestinationPath { get; init; } = string.Empty;

    public long FileSize { get; init; }

    public string SourceHash { get; init; } = string.Empty;

    public string DestinationHash { get; init; } = string.Empty;

    public bool Exists { get; init; }

    public bool HashMatches { get; init; }

    public bool Skipped { get; init; }

    public bool Verified => Exists && HashMatches;

    public DateTime Timestamp { get; init; } = DateTime.Now;

    public string Status =>
        Verified ? "Verified" :
        Skipped ? "Skipped" :
        !Exists ? "Missing" :
        "Failed";
}
