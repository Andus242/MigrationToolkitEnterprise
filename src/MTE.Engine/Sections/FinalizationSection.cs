using MTE.Core.Interfaces;

namespace MTE.Engine.Sections;

public sealed class FinalizationSection : IMigrationSection
{
    public string Name => "Finalization";

    public async Task ExecuteAsync(
        IProgress<MigrationProgressInfo> progress,
        CancellationToken cancellationToken)
    {
        progress.Report(new MigrationProgressInfo
        {
            Section = Name,
            Message = "Finalizing migration...",
            Percentage = 90,
            IsComplete = false
        });

        await Task.Delay(750, cancellationToken);

        progress.Report(new MigrationProgressInfo
        {
            Section = Name,
            Message = "Migration finalization completed.",
            Percentage = 95,
            IsComplete = false
        });
    }
}