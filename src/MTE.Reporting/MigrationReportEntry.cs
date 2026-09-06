using System;

namespace MTE.Reporting;

public sealed class MigrationReportEntry
{
    public string SourcePath { get; init; } = string.Empty;

    public string DestinationPath { get; init; } = string.Empty;

    public long FileSize { get; init; }

    public string SHA256 { get; init; } = string.Empty;

    public DateTime CopiedAt { get; init; } = DateTime.Now;

    public bool Copied { get; init; }

    public bool Verified { get; init; }

    public string Status { get; init; } = "Pending";

    public string ErrorMessage { get; init; } = string.Empty;
}
