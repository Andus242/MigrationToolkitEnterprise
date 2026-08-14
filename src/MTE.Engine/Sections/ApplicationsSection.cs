using MTE.Core.Interfaces;

namespace MTE.Engine.Sections;

public sealed class ApplicationsSection : IMigrationSection
{
    public string Name => "Applications";

    public async Task ExecuteAsync(
        IProgress<MigrationProgressInfo> progress,
        CancellationToken cancellationToken)
    {
        progress.Report(new MigrationProgressInfo
        {
            Section = Name,
            Message = "Preparing application settings...",
            Percentage = 55,
            IsComplete = false
        });

        await Task.Delay(750, cancellationToken);

        progress.Report(new MigrationProgressInfo
        {
            Section = Name,
            Message = "Application settings preparation completed.",
            Percentage = 60,
            IsComplete = false
        });
    }
}