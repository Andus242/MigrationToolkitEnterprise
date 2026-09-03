using MTE.Core.Models;

namespace MTE.Core.Interfaces;

public interface IMigrationStorageManager
{
    IReadOnlyList<MigrationStorageInfo> DetectRemovableDrives();

    MigrationStorageInfo? GetDrive(string rootPath);

    string CreateMigrationPackage(
        MigrationStorageInfo storage);
}