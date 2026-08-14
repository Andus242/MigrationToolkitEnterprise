namespace MTE.Core.Models;

public sealed class MigrationManifestEntry
{
    public string SourcePath { get; init; } = string.Empty;

    public string DestinationPath { get; init; } = string.Empty;

    public long FileSize { get; init; }

    public string SHA256 { get; init; } = string.Empty;

    public DateTime CopiedAt { get; init; } = DateTime.Now;

    public bool Verified { get; set; }

    public string VerificationStatus { get; set; } = "Pending";
}