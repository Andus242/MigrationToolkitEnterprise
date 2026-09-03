using System;
using System.IO;

namespace MTE.Engine.Services;

public sealed class MigrationStorageService
{
    public string CreateMigrationRoot(string driveLetter)
    {
        if (string.IsNullOrWhiteSpace(driveLetter))
        {
            throw new ArgumentException(
                "A destination drive is required.",
                nameof(driveLetter));
        }

        var root = driveLetter.TrimEnd('\\');

        var migrationRoot =
            Path.Combine(root, "MTE Migration");

        Directory.CreateDirectory(migrationRoot);

        CreateDirectory(migrationRoot, "Manifest");
        CreateDirectory(migrationRoot, "UserProfiles");

        CreateDirectory(migrationRoot, "Documents");
        CreateDirectory(migrationRoot, "Desktop");
        CreateDirectory(migrationRoot, "Downloads");
        CreateDirectory(migrationRoot, "Pictures");
        CreateDirectory(migrationRoot, "Videos");
        CreateDirectory(migrationRoot, "Music");
        CreateDirectory(migrationRoot, "Favorites");

        CreateDirectory(migrationRoot, "ApplicationSettings");
        CreateDirectory(migrationRoot, "BrowserData");
        CreateDirectory(migrationRoot, "Network");
        CreateDirectory(migrationRoot, "Printers");

        CreateDirectory(migrationRoot, "Verification");
        CreateDirectory(migrationRoot, "Reports");
        CreateDirectory(migrationRoot, "Logs");

        return migrationRoot;
    }

    private static void CreateDirectory(
        string migrationRoot,
        string directoryName)
    {
        Directory.CreateDirectory(
            Path.Combine(
                migrationRoot,
                directoryName));
    }
}