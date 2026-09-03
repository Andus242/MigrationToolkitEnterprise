using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MTE.Core.Models;

namespace MTE.Engine.Services;

public sealed class DriveDetectionService
{
    public IReadOnlyList<MigrationDestination> GetAvailableDrives()
    {
        var drives = new List<MigrationDestination>();

        string systemDrive = Path.GetPathRoot(
            Environment.SystemDirectory) ?? "C:\\";

        systemDrive = systemDrive.TrimEnd('\\');

        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            try
            {
                if (!drive.IsReady)
                    continue;

                if (drive.DriveType != DriveType.Fixed &&
                    drive.DriveType != DriveType.Removable &&
                    drive.DriveType != DriveType.Network)
                {
                    continue;
                }

                string driveLetter = drive.Name.TrimEnd('\\');

                bool isSystemDrive =
                    string.Equals(
                        driveLetter,
                        systemDrive,
                        StringComparison.OrdinalIgnoreCase);

                bool isRemovable =
                    drive.DriveType == DriveType.Removable;

                bool isExternal =
                    !isSystemDrive &&
                    (isRemovable || drive.DriveType == DriveType.Fixed);

                string driveName =
                    string.IsNullOrWhiteSpace(drive.VolumeLabel)
                        ? "Local Disk"
                        : drive.VolumeLabel;

                drives.Add(new MigrationDestination
                {
                    DriveLetter = driveLetter,
                    DriveName = driveName,
                    TotalBytes = drive.TotalSize,
                    FreeBytes = drive.AvailableFreeSpace,
                    IsSystemDrive = isSystemDrive,
                    IsRemovable = isRemovable,
                    IsExternal = isExternal
                });
            }
            catch
            {
                // Ignore drives that cannot be queried.
            }
        }

        return drives
            .OrderBy(d => d.IsSystemDrive)
            .ThenByDescending(d => d.IsRecommended)
            .ThenBy(d => d.DriveLetter)
            .ToList();
    }
}
