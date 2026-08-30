using MTE.Core.Models;

namespace MTE.Core.Interfaces;

public interface IMigrationEngine
{
    Task ExecuteAsync(
        string destinationDrive,
        IReadOnlyList<MigrationProfileSelection> selectedProfiles,
        IProgress<MigrationProgressInfo> progress,
        CancellationToken cancellationToken);
}
