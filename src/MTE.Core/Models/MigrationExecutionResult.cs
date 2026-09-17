using System.Collections.Generic;
using System.Linq;

namespace MTE.Core.Models;

public sealed class MigrationExecutionResult
{
    public List<VerificationResult> FileResults { get; init; } = new();

    public List<MigrationSectionResult> Sections { get; init; } = new();

    public int TotalFiles =>
        FileResults.Count;

    public long TotalBytes =>
        FileResults.Sum(result => result.FileSize);

    public int CopiedFiles =>
        FileResults.Count(result => result.Verified);

    public long CopiedBytes =>
        FileResults
            .Where(result => result.Verified)
            .Sum(result => result.FileSize);

    public int VerifiedFiles =>
        FileResults.Count(result => result.Verified);

    public int FailedFiles =>
        FileResults.Count(result => !result.Verified);
}
