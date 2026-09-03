using System.IO;
using MTE.Core.Interfaces;
using MTE.Core.Models;

namespace MTE.Engine.Storage;

public sealed class MigrationStorageManager : IMigrationStorageManager
{
    public IReadOnlyList<MigrationStorageInfo> DetectRemovableDrives()
    {
        var results = new List<MigrationStorageInfo>();

        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                if (!drive.IsReady)
                    continue;

                if (drive.DriveType != DriveType.Removable)
                    continue;

                results.Add(
                    new MigrationStorageInfo
                    {
                        DriveLetter = drive.Name.TrimEnd('\\'),
                        DriveName = string.IsNullOrWhiteSpace(
                            drive.VolumeLabel)
                            ? "Removable Drive"
                            : drive.VolumeLabel,
                        RootPath = drive.RootDirectory.FullName,
                        TotalBytes = drive.TotalSize,
                        AvailableBytes = drive.AvailableFreeSpace,
                        IsReady = true
                    });
            }
            catch
            {
                // Ignore drives that cannot be queried.
            }
        }

        return results;
    }

    public MigrationStorageInfo? GetDrive(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
            return null;

        try
        {
            var drive = new DriveInfo(rootPath);

            if (!drive.IsReady)
                return null;

            return new MigrationStorageInfo
            {
                DriveLetter = drive.Name.TrimEnd('\\'),
                DriveName = string.IsNullOrWhiteSpace(
                    drive.VolumeLabel)
                    ? "Drive"
                    : drive.VolumeLabel,
                RootPath = drive.RootDirectory.FullName,
                TotalBytes = drive.TotalSize,
                AvailableBytes = drive.AvailableFreeSpace,
                IsReady = true
            };
        }
        catch
        {
            return null;
        }
    }

    public string CreateMigrationPackage(
        MigrationStorageInfo storage)
    {
        if (!storage.IsReady)
            throw new InvalidOperationException(
                "The selected migration drive is not ready.");

        var computerName = Environment.MachineName;

        var timestamp =
            DateTime.Now.ToString("yyyyMMdd-HHmmss");

        var packageName =
            $"{computerName}-{timestamp}";

        var packagePath = Path.Combine(
            storage.RootPath,
            "MTE-Migrations",
            packageName);

        Directory.CreateDirectory(packagePath);

        Directory.CreateDirectory(
            Path.Combine(packagePath, "Manifest"));

        Directory.CreateDirectory(
            Path.Combine(packagePath, "Users"));

        Directory.CreateDirectory(
            Path.Combine(packagePath, "Applications"));

        Directory.CreateDirectory(
            Path.Combine(packagePath, "Network"));

        Directory.CreateDirectory(
            Path.Combine(packagePath, "Logs"));

        Directory.CreateDirectory(
            Path.Combine(packagePath, "Reports"));

        return packagePath;
    }
}