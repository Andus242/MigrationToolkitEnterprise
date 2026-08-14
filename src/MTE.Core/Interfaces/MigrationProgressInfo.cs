namespace MTE.Core.Interfaces;

public sealed class MigrationProgressInfo
{
    public string Section { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public int Percentage { get; init; }

    public bool IsComplete { get; init; }
}