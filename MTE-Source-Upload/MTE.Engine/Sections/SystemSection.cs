using MTE.Core.Interfaces;

namespace MTE.Engine.Sections;

public sealed class SystemSection : IMigrationSection
{
    private readonly ISystemDiscoveryService _discoveryService;

    public SystemSection(
        ISystemDiscoveryService discoveryService)
    {
        _discoveryService = discoveryService;
    }

    public string Name => "System";

    public async Task ExecuteAsync(
        IProgress<MigrationProgressInfo> progress,
        CancellationToken cancellationToken)
    {
        progress.Report(new MigrationProgressInfo
        {
            Section = Name,
            Message = "Discovering Windows system...",
            Percentage = 10,
            IsComplete = false
        });

        var system = await _discoveryService.DiscoverAsync(
            cancellationToken);

        progress.Report(new MigrationProgressInfo
        {
            Section = Name,
            Message =
                $"Computer: {system.ComputerName} | " +
                $"OS: {system.OperatingSystem} | " +
                $"Architecture: {system.Architecture}",
            Percentage = 15,
            IsComplete = false
        });

        await Task.Delay(300, cancellationToken);

        progress.Report(new MigrationProgressInfo
        {
            Section = Name,
            Message =
                $"User: {system.CurrentUser} | " +
                $"Processors: {system.ProcessorCount}",
            Percentage = 20,
            IsComplete = false
        });
    }
}