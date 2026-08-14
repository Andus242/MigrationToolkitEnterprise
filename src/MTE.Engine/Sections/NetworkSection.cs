using MTE.Core.Interfaces;

namespace MTE.Engine.Sections;

public sealed class NetworkSection : IMigrationSection
{
    public string Name => "Network";

    public async Task ExecuteAsync(
        IProgress<MigrationProgressInfo> progress,
        CancellationToken cancellationToken)
    {
        progress.Report(new MigrationProgressInfo
        {
            Section = Name,
            Message = "Preparing network configuration...",
            Percentage = 70,
            IsComplete = false
        });

        await Task.Delay(750, cancellationToken);

        progress.Report(new MigrationProgressInfo
        {
            Section = Name,
            Message = "Network configuration preparation completed.",
            Percentage = 75,
            IsComplete = false
        });
    }
}