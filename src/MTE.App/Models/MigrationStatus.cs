namespace MTE.App.Models;

public enum MigrationStatus
{
    Ready,
    Preparing,
    Running,
    Completed,
    Failed,
    Cancelled
}