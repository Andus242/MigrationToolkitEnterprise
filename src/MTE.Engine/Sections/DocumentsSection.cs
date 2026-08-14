using MTE.Core.Interfaces;

namespace MTE.Engine.Sections;

public sealed class DocumentsSection : IMigrationSection
{
    public string Name => "Documents";

    public async Task ExecuteAsync(
        IProgress<MigrationProgressInfo> progress,
        CancellationToken cancellationToken)
    {
        progress.Report(new MigrationProgressInfo
        {
            Section = Name,
            Message = "Preparing document migration...",
            Percentage = 40,
            IsComplete = false
        });

        await Task.Delay(750, cancellationToken);

        progress.Report(new MigrationProgressInfo
        {
            Section = Name,
            Message = "Document migration preparation completed.",
            Percentage = 45,
            IsComplete = false
        });
    }
}