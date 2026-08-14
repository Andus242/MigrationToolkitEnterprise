using System.Runtime.InteropServices;
using MTE.Core.Interfaces;

namespace MTE.Engine.Sections.Discovery;

public sealed class SystemDiscoverySection
{
    public async Task ExecuteAsync(
        IProgress<MigrationProgressInfo> progress,
        CancellationToken cancellationToken)
    {
        Report(
            progress,
            "System Discovery",
            "Starting system discovery...",
            0);

        await Task.Delay(300, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var computerName = Environment.MachineName;

        Report(
            progress,
            "System Discovery",
            $"Computer: {computerName}",
            10);

        await Task.Delay(300, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var userName = Environment.UserName;

        Report(
            progress,
            "System Discovery",
            $"Current user: {userName}",
            20);

        await Task.Delay(300, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var operatingSystem = RuntimeInformation.OSDescription;

        Report(
            progress,
            "System Discovery",
            $"Operating system: {operatingSystem}",
            35);

        await Task.Delay(300, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var architecture = RuntimeInformation.OSArchitecture;

        Report(
            progress,
            "System Discovery",
            $"Architecture: {architecture}",
            50);

        await Task.Delay(300, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var processorCount = Environment.ProcessorCount;

        Report(
            progress,
            "System Discovery",
            $"Logical processors: {processorCount}",
            65);

        await Task.Delay(300, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var systemDrive =
            Environment.GetEnvironmentVariable("SystemDrive")
            ?? "Unknown";

        Report(
            progress,
            "System Discovery",
            $"System drive: {systemDrive}",
            80);

        await Task.Delay(300, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        Report(
            progress,
            "System Discovery",
            "System discovery completed.",
            100);
    }

    private static void Report(
        IProgress<MigrationProgressInfo> progress,
        string section,
        string message,
        int percentage)
    {
        progress.Report(new MigrationProgressInfo
        {
            Section = section,
            Message = message,
            Percentage = percentage,
            IsComplete = percentage >= 100
        });
    }
}