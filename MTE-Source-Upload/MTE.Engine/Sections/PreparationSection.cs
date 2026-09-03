using MTE.Core.Interfaces;

namespace MTE.Engine.Sections;

public sealed class PreparationSection : IMigrationSection
{
    public string Name => "Preparation";

    public async Task ExecuteAsync(
        IProgress<MigrationProgressInfo> progress,
        CancellationToken cancellationToken)
    {
        progress.Report(new MigrationProgressInfo
        {
            Section = Name,
            Message = "Preparing migration...",
            Percentage = 0,
            IsComplete = false
        });

        await Task.Delay(500, cancellationToken);

        progress.Report(new MigrationProgressInfo
        {
            Section = Name,
            Message = "Migration preparation completed.",
            Percentage = 5,
            IsComplete = false
        });
    }
}