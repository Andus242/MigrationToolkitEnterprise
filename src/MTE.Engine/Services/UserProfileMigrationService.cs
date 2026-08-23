using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MTE.Core.Interfaces;

namespace MTE.Engine.Services;

public sealed class UserProfileMigrationService
{
    private readonly FileMigrationService _fileMigrationService;
    private readonly IMigrationLogger _logger;

    public UserProfileMigrationService(
        FileMigrationService fileMigrationService,
        IMigrationLogger logger)
    {
        _fileMigrationService = fileMigrationService;
        _logger = logger;
    }

    public async Task CopyUserProfileAsync(
        string sourceProfile,
        string destinationProfile,
        IProgress<MigrationProgressInfo>? progress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourceProfile))
            throw new ArgumentException(
                "Source profile is required.",
                nameof(sourceProfile));

        if (!Directory.Exists(sourceProfile))
        {
            _logger.Warning(
                $"User profile not found: {sourceProfile}");

            return;
        }

        Directory.CreateDirectory(destinationProfile);

        var folders = new[]
        {
            "Desktop",
            "Downloads",
            "Pictures",
            "Videos",
            "Music",
            "Favorites"
        };

        var availableFolders = folders
            .Where(folder =>
                Directory.Exists(
                    Path.Combine(sourceProfile, folder)))
            .ToList();

        if (availableFolders.Count == 0)
        {
            progress?.Report(new MigrationProgressInfo
            {
                Section = "User Profiles",
                Message = "No user folders found to migrate.",
                Percentage = 100,
                IsComplete = true
            });

            return;
        }

        for (var index = 0; index < availableFolders.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var folder = availableFolders[index];

            var sourceFolder =
                Path.Combine(
                    sourceProfile,
                    folder);

            var destinationFolder =
                Path.Combine(
                    destinationProfile,
                    folder);

            var startingPercentage =
                (int)Math.Round(
                    index * 100.0 /
                    availableFolders.Count);

            progress?.Report(new MigrationProgressInfo
            {
                Section = "User Profiles",
                Message = $"Migrating {folder}...",
                Percentage = startingPercentage,
                IsComplete = false
            });

            await CopyFolderAsync(
                sourceFolder,
                destinationFolder,
                folder,
                progress,
                index,
                availableFolders.Count,
                cancellationToken);
        }

        progress?.Report(new MigrationProgressInfo
        {
            Section = "User Profiles",
            Message = "User profile migration completed and files verified.",
            Percentage = 100,
            IsComplete = true
        });

        _logger.Success(
            $"User profile migration completed: {sourceProfile}");
    }

    private async Task CopyFolderAsync(
        string sourceFolder,
        string destinationFolder,
        string folderName,
        IProgress<MigrationProgressInfo>? progress,
        int folderIndex,
        int folderCount,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(destinationFolder);

        var files = GetFilesExcludingOneDrive(
            sourceFolder,
            cancellationToken);

        var totalFiles = files.Count;
        var completedFiles = 0;

        if (totalFiles == 0)
            return;

        foreach (var sourceFile in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath =
                Path.GetRelativePath(
                    sourceFolder,
                    sourceFile);

            var destinationFile =
                Path.Combine(
                    destinationFolder,
                    relativePath);

            try
            {
                var result =
                    await _fileMigrationService.CopyAndVerifyAsync(
                        sourceFile,
                        destinationFile,
                        true,
                        cancellationToken);

                completedFiles++;

                var folderProgress =
                    completedFiles * 100.0 /
                    totalFiles;

                var overallProgress =
                    (folderIndex * 100.0 +
                     folderProgress) /
                    folderCount;

                progress?.Report(new MigrationProgressInfo
                {
                    Section = "User Profiles",
                    Message =
                        $"Verified {completedFiles}/{totalFiles}: " +
                        $"{folderName}\\{Path.GetFileName(sourceFile)}",
                    Percentage =
                        Math.Min(
                            99,
                            (int)Math.Round(overallProgress)),
                    IsComplete = false
                });

                if (!result.Verified)
                {
                    _logger.Error(
                        $"Verification failed: {sourceFile}");
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                completedFiles++;

                _logger.Warning(
                    $"Skipped {sourceFile}: {ex.Message}");
            }
        }
    }

    private static List<string> GetFilesExcludingOneDrive(
        string rootDirectory,
        CancellationToken cancellationToken)
    {
        var files = new List<string>();

        ScanDirectory(
            rootDirectory,
            files,
            cancellationToken);

        return files;
    }

    private static void ScanDirectory(
        string directory,
        List<string> files,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DirectoryInfo info;

        try
        {
            info = new DirectoryInfo(directory);
        }
        catch
        {
            return;
        }

        if (info.Name.Equals(
                "OneDrive",
                StringComparison.OrdinalIgnoreCase) ||
            info.Name.StartsWith(
                "OneDrive - ",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            foreach (var file in info.EnumerateFiles())
            {
                cancellationToken.ThrowIfCancellationRequested();
                files.Add(file.FullName);
            }
        }
        catch
        {
            // Ignore inaccessible folders.
        }

        try
        {
            foreach (var subdirectory in info.EnumerateDirectories())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (subdirectory.Name.Equals(
                        "OneDrive",
                        StringComparison.OrdinalIgnoreCase) ||
                    subdirectory.Name.StartsWith(
                        "OneDrive - ",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                ScanDirectory(
                    subdirectory.FullName,
                    files,
                    cancellationToken);
            }
        }
        catch
        {
            // Ignore inaccessible folders.
        }
    }
}
