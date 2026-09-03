namespace MTE.Core.Models;

public sealed class MigrationDestination
{
    public string DriveLetter { get; set; } = string.Empty;
    public string DriveName { get; set; } = string.Empty;
    public long TotalBytes { get; set; }
    public long FreeBytes { get; set; }

    public bool IsExternal { get; set; }
    public bool IsRemovable { get; set; }
    public bool IsSystemDrive { get; set; }

    public long UsedBytes =>
        Math.Max(0, TotalBytes - FreeBytes);

    public double FreePercentage =>
        TotalBytes > 0
            ? (double)FreeBytes / TotalBytes * 100
            : 0;

    public string DriveTypeDisplay
    {
        get
        {
            if (IsSystemDrive)
                return "SYSTEM DRIVE";

            if (IsRemovable)
                return "USB / REMOVABLE";

            if (IsExternal)
                return "EXTERNAL";

            return "INTERNAL";
        }
    }

    public bool IsRecommended =>
        IsExternal &&
        !IsSystemDrive &&
        FreeBytes > 0;

    public string DisplayName =>
        $"{DriveLetter} - {DriveName} " +
        $"[{DriveTypeDisplay}] " +
        $"({FormatBytes(FreeBytes)} free of {FormatBytes(TotalBytes)})";

    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1024L * 1024 * 1024 * 1024)
            return $"{bytes / (1024d * 1024 * 1024 * 1024):F1} TB";

        if (bytes >= 1024L * 1024 * 1024)
            return $"{bytes / (1024d * 1024 * 1024):F1} GB";

        if (bytes >= 1024L * 1024)
            return $"{bytes / (1024d * 1024):F1} MB";

        if (bytes >= 1024)
            return $"{bytes / 1024d:F1} KB";

        return $"{bytes} bytes";
    }
}