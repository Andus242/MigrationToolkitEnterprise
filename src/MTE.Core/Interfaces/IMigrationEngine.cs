namespace MTE.Core.Interfaces;

public interface IMigrationEngine
{
    Task ExecuteAsync(
        IProgress<MigrationProgressInfo> progress,
        CancellationToken cancellationToken);
}