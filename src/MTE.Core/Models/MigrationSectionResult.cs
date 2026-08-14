namespace MTE.Core.Models;

public sealed class MigrationSectionResult
{
    public string Section { get; init; } = string.Empty;

    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public DateTime Timestamp { get; init; } = DateTime.Now;
}