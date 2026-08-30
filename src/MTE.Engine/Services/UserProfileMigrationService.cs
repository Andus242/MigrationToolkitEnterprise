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
        IReadOnlyList<string>? selectedFolders,
        IProgress<MigrationProgressInfo>? progress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourceProfile))
        {
            throw new ArgumentException(
                "Source profile is required.",
                nameof(sourceProfile));
        }

        if (!Directory.Exists(sourceProfile))
        {
            _logger.Warning(
                $"User profile not found: {sourceProfile}");

            progress?.Report(new MigrationProgressInfo
            {
                Section = "User Profiles",
                Message = $"User profile not found: {sourceProfile}",
                Percentage = 100,
                IsComplete = true
            });

            return;
        }

        Directory.CreateDirectory(destinationProfile);

        var standardFolders = new[]
        {
            "Desktop",
            "Documents",
            "Downloads",
            "Pictures",
            "Videos",
            "Music",
            "Favorites"
        };

        // null means "Whole Profile".
        // An empty list means "Selected Folders" with nothing selected.
        // Otherwise, only the folders explicitly selected for this profile
        // are eligible for migration.
        var folders = selectedFolders is null
            ? standardFolders
            : standardFolders
                .Where(folder =>
                    selectedFolders.Contains(
                        folder,
                        StringComparer.OrdinalIgnoreCase))
                .ToArray();

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

            _logger.Warning(
                $"No standard user folders found in: {sourceProfile}");

            return;
        }

        for (var index = 0;
             index < availableFolders.Count;
             index++)
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

            Directory.CreateDirectory(destinationFolder);

            ReportFolderProgress(
                progress,
                folder,
                index,
                availableFolders.Count,
                0,
                $"Preparing {folder}...",
                false);

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
        var lastReportedProgress = 0;

        if (totalFiles == 0)
        {
            ReportFolderProgress(
                progress,
                folderName,
                folderIndex,
                folderCount,
                100,
                $"{folderName} is empty - nothing to copy.",
                false);

            return;
        }

        ReportFolderProgress(
            progress,
            folderName,
            folderIndex,
            folderCount,
            0,
            $"Migrating {folderName} ({totalFiles:N0} file(s))...",
            false);

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

            var destinationDirectory =
                Path.GetDirectoryName(destinationFile);

            if (!string.IsNullOrWhiteSpace(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            try
            {
                var currentFileName =
                    Path.GetFileName(sourceFile);

                var fileProgress =
                    new Progress<double>(percentage =>
                    {
                        var safeFilePercentage =
                            Math.Clamp(
                                percentage,
                                0d,
                                100d);

                        var folderProgress =
                            ((completedFiles +
                              safeFilePercentage / 100d) /
                             totalFiles) *
                            100d;

                        var overallProgress =
                            CalculateOverallProgress(
                                folderIndex,
                                folderCount,
                                folderProgress);

                        var reportedProgress =
                            Math.Clamp(
                                (int)Math.Round(overallProgress),
                                0,
                                99);

                        if (reportedProgress < lastReportedProgress)
                        {
                            reportedProgress = lastReportedProgress;
                        }
                        else
                        {
                            lastReportedProgress = reportedProgress;
                        }

                        progress?.Report(new MigrationProgressInfo
                        {
                            Section = "User Profiles",
                            Message =
                                $"Copying/verifying: " +
                                $"{folderName}\\{currentFileName}",
                            Percentage = reportedProgress,
                            IsComplete = false
                        });
                    });

                var result =
                    await _fileMigrationService.CopyAndVerifyAsync(
                        sourceFile,
                        destinationFile,
                        fileProgress,
                        cancellationToken);

                completedFiles++;

                var completedFolderProgress =
                    completedFiles * 100d / totalFiles;

                var completedOverallProgress =
                    CalculateOverallProgress(
                        folderIndex,
                        folderCount,
                        completedFolderProgress);

                var completedReportedProgress =
                    Math.Clamp(
                        (int)Math.Round(completedOverallProgress),
                        0,
                        99);

                if (completedReportedProgress < lastReportedProgress)
                {
                    completedReportedProgress =
                        lastReportedProgress;
                }
                else
                {
                    lastReportedProgress =
                        completedReportedProgress;
                }

                var verificationText =
                    result.Verified
                        ? "Verified"
                        : "Verification failed";

                progress?.Report(new MigrationProgressInfo
                {
                    Section = "User Profiles",
                    Message =
                        $"{verificationText} " +
                        $"{completedFiles:N0}/{totalFiles:N0}: " +
                        $"{folderName}\\{Path.GetFileName(sourceFile)}",
                    Percentage = completedReportedProgress,
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

                var failedFolderProgress =
                    completedFiles * 100d / totalFiles;

                var failedOverallProgress =
                    CalculateOverallProgress(
                        folderIndex,
                        folderCount,
                        failedFolderProgress);

                var failedReportedProgress =
                    Math.Clamp(
                        (int)Math.Round(failedOverallProgress),
                        0,
                        99);

                if (failedReportedProgress < lastReportedProgress)
                {
                    failedReportedProgress =
                        lastReportedProgress;
                }
                else
                {
                    lastReportedProgress =
                        failedReportedProgress;
                }

                progress?.Report(new MigrationProgressInfo
                {
                    Section = "User Profiles",
                    Message =
                        $"Skipped file {completedFiles:N0}/{totalFiles:N0}: " +
                        $"{folderName}\\{Path.GetFileName(sourceFile)}",
                    Percentage = failedReportedProgress,
                    IsComplete = false
                });

                _logger.Warning(
                    $"Skipped {sourceFile}: {ex.Message}");
            }
        }

        ReportFolderProgress(
            progress,
            folderName,
            folderIndex,
            folderCount,
            100,
            $"{folderName} completed and files verified.",
            false);
    }

    private static void ReportFolderProgress(
        IProgress<MigrationProgressInfo>? progress,
        string folderName,
        int folderIndex,
        int folderCount,
        double folderPercentage,
        string message,
        bool isComplete)
    {
        var overallProgress =
            CalculateOverallProgress(
                folderIndex,
                folderCount,
                folderPercentage);

        var percentage =
            Math.Clamp(
                (int)Math.Round(overallProgress),
                0,
                100);

        progress?.Report(new MigrationProgressInfo
        {
            Section = "User Profiles",
            Message = message,
            Percentage = percentage,
            IsComplete = isComplete
        });
    }

    private static double CalculateOverallProgress(
        int folderIndex,
        int folderCount,
        double folderPercentage)
    {
        if (folderCount <= 0)
        {
            return 100;
        }

        var safeFolderPercentage =
            Math.Clamp(
                folderPercentage,
                0d,
                100d);

        return
            ((folderIndex * 100d) +
             safeFolderPercentage) /
            folderCount;
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
            // Ignore inaccessible folders/files.
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


