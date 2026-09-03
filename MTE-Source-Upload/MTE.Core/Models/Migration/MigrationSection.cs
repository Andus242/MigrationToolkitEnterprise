namespace MTE.Core.Models.Migration;

public sealed class MigrationSection
{
    public string Name { get; set; } = string.Empty;

    public string Status { get; set; } = "Pending";

    public int ProgressPercentage { get; set; }

    public bool IsComplete { get; set; }

    public bool IsRunning { get; set; }

    public bool IsSkipped { get; set; }

    public string Message { get; set; } = string.Empty;
}