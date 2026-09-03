namespace MTE.Core.Models;

public sealed class SystemInformation
{
    public string ComputerName { get; init; } = string.Empty;

    public string OperatingSystem { get; init; } = string.Empty;

    public string WindowsVersion { get; init; } = string.Empty;

    public string Architecture { get; init; } = string.Empty;

    public string CurrentUser { get; init; } = string.Empty;

    public string WindowsDirectory { get; init; } = string.Empty;

    public string SystemDrive { get; init; } = string.Empty;

    public int ProcessorCount { get; init; }

    public long TotalMemoryBytes { get; init; }

    public DateTime DiscoveredAt { get; init; } = DateTime.Now;
}