namespace MTE.App.Models;

public sealed class MigrationProgress
{
    public string Section { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public int Percentage { get; init; }

    public MigrationStatus Status { get; init; }

    public DateTime Timestamp { get; init; } = DateTime.Now;
}