namespace MTE.Core.Models;

public sealed class MigrationStorageInfo
{
    public string DriveLetter { get; init; } = string.Empty;

    public string DriveName { get; init; } = string.Empty;

    public string RootPath { get; init; } = string.Empty;

    public long TotalBytes { get; init; }

    public long AvailableBytes { get; init; }

    public bool IsReady { get; init; }

    public string DisplayCapacity =>
        FormatBytes(TotalBytes);

    public string DisplayAvailable =>
        FormatBytes(AvailableBytes);

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";

        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F1} KB";

        if (bytes < 1024L * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024):F1} MB";

        if (bytes < 1024L * 1024 * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";

        return $"{bytes / (1024.0 * 1024 * 1024 * 1024):F2} TB";
    }
}