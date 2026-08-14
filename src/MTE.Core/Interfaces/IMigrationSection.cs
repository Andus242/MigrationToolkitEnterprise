namespace MTE.Core.Interfaces;

public interface IMigrationSection
{
    string Name { get; }

    Task ExecuteAsync(
        IProgress<MigrationProgressInfo> progress,
        CancellationToken cancellationToken);
}