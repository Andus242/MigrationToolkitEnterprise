using System;
using System.Collections.Generic;
using MTE.Core.Models;

namespace MTE.Reporting;

public sealed class MigrationReport
{
    public string ReportVersion { get; init; } = "1.0";

    public string MigrationId { get; init; } =
        Guid.NewGuid().ToString("N");

    public string ComputerName { get; init; } =
        Environment.MachineName;

    public string Destination { get; init; } = string.Empty;

    public DateTime StartedAt { get; init; } = DateTime.Now;

    public DateTime? CompletedAt { get; set; }

    public string Status { get; set; } = "In Progress";

    public string MigrationMode { get; init; } = string.Empty;

    public int TotalFiles { get; set; }

    public long TotalBytes { get; set; }

    public int CopiedFiles { get; set; }

    public long CopiedBytes { get; set; }

    public int VerifiedFiles { get; set; }

    public int FailedFiles { get; set; }

    public int SkippedFiles { get; set; }

    public List<string> SelectedProfiles { get; init; } = new();

    public List<string> SelectedFolders { get; init; } = new();

    public List<MigrationManifestEntry> Files { get; init; } = new();

    public List<MigrationSectionResult> Sections { get; init; } = new();
}
