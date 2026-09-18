using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MTE.Core.Interfaces;
using MTE.Core.Models;

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

    public async Task<List<VerificationResult>> CopyUserProfileAsync(
        string sourceProfile,
        string destinationProfile,
        IReadOnlyList<string>? selectedFolders,
        IProgress<MigrationProgressInfo>? progress,
        CancellationToken cancellationToken)
    {
        var results = new List<VerificationResult>();

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

            return results;
        }

        Directory.CreateDirectory(destinationProfile);

        // null means "Whole Profile".
        // An empty list means "Selected Folders" with nothing selected.
        if (selectedFolders is null)
        {
            var wholeProfileResults =
                await CopyWholeProfileAsync(
                    sourceProfile,
                    destinationProfile,
                    progress,
                    cancellationToken);

            results.AddRange(wholeProfileResults);

            return results;
        }

        var selectedFolderResults =
            await CopySelectedFoldersAsync(
                sourceProfile,
                destinationProfile,
                selectedFolders,
                progress,
                cancellationToken);

        results.AddRange(selectedFolderResults);

        return results;
    }

    private async Task<List<VerificationResult>> CopyWholeProfileAsync(
        string sourceProfile,
        string destinationProfile,
        IProgress<MigrationProgressInfo>? progress,
        CancellationToken cancellationToken)
    {
        var results = new List<VerificationResult>();

        progress?.Report(new MigrationProgressInfo
        {
            Section = "User Profiles",
            Message = "Scanning complete user profile...",
            Percentage = 0,
            IsComplete = false
        });

        var files = GetWholeProfileFiles(
            sourceProfile,
            cancellationToken);

        var totalFiles = files.Count;
        var completedFiles = 0;
        var lastReportedProgress = 0;

        if (totalFiles == 0)
        {
            progress?.Report(new MigrationProgressInfo
            {
                Section = "User Profiles",
                Message = "No eligible profile files found to migrate.",
                Percentage = 100,
                IsComplete = true
            });

            _logger.Warning(
                $"No eligible profile files found in: {sourceProfile}");

            return results;
        }

        progress?.Report(new MigrationProgressInfo
        {
            Section = "User Profiles",
            Message =
                $"Migrating whole profile ({totalFiles:N0} file(s))...",
            Percentage = 0,
            IsComplete = false
        });

        foreach (var sourceFile in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath =
                Path.GetRelativePath(
                    sourceProfile,
                    sourceFile);

            var destinationFile =
                Path.Combine(
                    destinationProfile,
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

                        var overallProgress =
                            ((completedFiles +
                              safeFilePercentage / 100d) /
                             totalFiles) *
                            100d;

                        var reportedProgress =
                            Math.Clamp(
                                (int)Math.Round(overallProgress),
                                0,
                                99);

                        
                        if (reportedProgress < lastReportedProgress)
                        {
                            reportedProgress =
                                lastReportedProgress;
                        }

                        if (reportedProgress != lastReportedProgress)
                        {
                            lastReportedProgress =
                                reportedProgress;

                            progress?.Report(new MigrationProgressInfo
                        {
                            Section = "User Profiles",
                            Message =
                                $"Copying/verifying: {relativePath}",
                            Percentage = reportedProgress,
                            IsComplete = false
                        });
                        }
                    });

                var result =
                    await _fileMigrationService.CopyAndVerifyAsync(
                        sourceFile,
                        destinationFile,
                        fileProgress,
                        cancellationToken);

                results.Add(result);

                completedFiles++;

                var completedPercentage =
                    Math.Clamp(
                        (int)Math.Round(
                            completedFiles * 100d / totalFiles),
                        0,
                        99);

                
                if (completedPercentage < lastReportedProgress)
                {
                    completedPercentage =
                        lastReportedProgress;
                }

                var verificationText =
                    result.Verified
                        ? "Verified"
                        : "Verification failed";

                if (completedPercentage != lastReportedProgress)
                {
                    lastReportedProgress =
                        completedPercentage;

                    progress?.Report(new MigrationProgressInfo
                    {
                        Section = "User Profiles",
                        Message =
                            $"{verificationText} " +
                            $"{completedFiles:N0}/{totalFiles:N0}: " +
                            $"{relativePath}",
                        Percentage = completedPercentage,
                        IsComplete = false
                    });
                }

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

                var failedPercentage =
                    Math.Clamp(
                        (int)Math.Round(
                            completedFiles * 100d / totalFiles),
                        0,
                        99);

                if (failedPercentage < lastReportedProgress)
                {
                    failedPercentage =
                        lastReportedProgress;
                }
                else
                {
                    lastReportedProgress =
                        failedPercentage;
                }

                results.Add(
                    new VerificationResult
                    {
                        SourcePath = sourceFile,
                        DestinationPath = destinationFile,
                        FileSize =
                            File.Exists(sourceFile)
                                ? new FileInfo(sourceFile).Length
                                : 0,
                        Exists = false,
                        HashMatches = false,
                        Skipped = true,
                        Timestamp = DateTime.Now
                    });

                progress?.Report(new MigrationProgressInfo
                {
                    Section = "User Profiles",
                    Message =
                        $"Skipped file " +
                        $"{completedFiles:N0}/{totalFiles:N0}: " +
                        $"{relativePath}",
                    Percentage = failedPercentage,
                    IsComplete = false
                });

                _logger.Warning(
                    $"Skipped {sourceFile}: {ex.Message}");
            }
        }

        progress?.Report(new MigrationProgressInfo
        {
            Section = "User Profiles",
            Message =
                "Whole user profile migration completed and files verified.",
            Percentage = 100,
            IsComplete = true
        });

        _logger.Success(
            $"Whole user profile migration completed: {sourceProfile}");

        return results;
    }

    private async Task<List<VerificationResult>> CopySelectedFoldersAsync(
        string sourceProfile,
        string destinationProfile,
        IReadOnlyList<string> selectedFolders,
        IProgress<MigrationProgressInfo>? progress,
        CancellationToken cancellationToken)
    {
        var results = new List<VerificationResult>();

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

        var folders =
            standardFolders
                .Where(folder =>
                    selectedFolders.Contains(
                        folder,
                        StringComparer.OrdinalIgnoreCase))
                .ToArray();

        var availableFolders =
            folders
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
                $"No selected user folders found in: {sourceProfile}");

            return results;
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

            var folderResults =
                await CopyFolderAsync(
                    sourceFolder,
                    destinationFolder,
                    folder,
                    progress,
                    index,
                    availableFolders.Count,
                    cancellationToken);

            results.AddRange(folderResults);
        }

        progress?.Report(new MigrationProgressInfo
        {
            Section = "User Profiles",
            Message =
                "Selected user folders migration completed and files verified.",
            Percentage = 100,
            IsComplete = true
        });

        _logger.Success(
            $"Selected user folders migration completed: {sourceProfile}");

        return results;
    }

    private async Task<List<VerificationResult>> CopyFolderAsync(
        string sourceFolder,
        string destinationFolder,
        string folderName,
        IProgress<MigrationProgressInfo>? progress,
        int folderIndex,
        int folderCount,
        CancellationToken cancellationToken)
    {
        var results = new List<VerificationResult>();

        Directory.CreateDirectory(destinationFolder);

        var files =
            GetFilesExcludingOneDrive(
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

            return results;
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
                            reportedProgress =
                                lastReportedProgress;
                        }
                        else
                        {
                            lastReportedProgress =
                                reportedProgress;
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

                results.Add(result);

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

                results.Add(
                    new VerificationResult
                    {
                        SourcePath = sourceFile,
                        DestinationPath = destinationFile,
                        FileSize =
                            File.Exists(sourceFile)
                                ? new FileInfo(sourceFile).Length
                                : 0,
                        Exists = false,
                        HashMatches = false,
                        Skipped = true,
                        Timestamp = DateTime.Now
                    });

                progress?.Report(new MigrationProgressInfo
                {
                    Section = "User Profiles",
                    Message =
                        $"Skipped file " +
                        $"{completedFiles:N0}/{totalFiles:N0}: " +
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

        return results;
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

    private static List<string> GetWholeProfileFiles(
        string rootDirectory,
        CancellationToken cancellationToken)
    {
        var files = new List<string>();

        ScanWholeProfileDirectory(
            rootDirectory,
            files,
            cancellationToken);

        return files;
    }

    private static void ScanWholeProfileDirectory(
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

        if (ShouldExcludeWholeProfileDirectory(info.Name))
        {
            return;
        }

        try
        {
            foreach (var file in info.EnumerateFiles())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (ShouldExcludeWholeProfileFile(file.Name))
                {
                    continue;
                }

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

                if (ShouldExcludeWholeProfileDirectory(
                        subdirectory.Name))
                {
                    continue;
                }

                ScanWholeProfileDirectory(
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

    private static bool ShouldExcludeWholeProfileDirectory(
        string directoryName)
    {
        if (directoryName.Equals(
                "AppData",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (directoryName.Equals(
                "OneDrive",
                StringComparison.OrdinalIgnoreCase) ||
            directoryName.StartsWith(
                "OneDrive - ",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return directoryName.Equals(
                   "Temp",
                   StringComparison.OrdinalIgnoreCase) ||
               directoryName.Equals(
                   "Temporary Internet Files",
                   StringComparison.OrdinalIgnoreCase) ||
               directoryName.Equals(
                   "INetCache",
                   StringComparison.OrdinalIgnoreCase) ||
               directoryName.Equals(
                   "WebCache",
                   StringComparison.OrdinalIgnoreCase) ||
               directoryName.Equals(
                   "Cache",
                   StringComparison.OrdinalIgnoreCase) ||
               directoryName.Equals(
                   "Caches",
                   StringComparison.OrdinalIgnoreCase) ||
               directoryName.Equals(
                   "Code Cache",
                   StringComparison.OrdinalIgnoreCase) ||
               directoryName.Equals(
                   "GPUCache",
                   StringComparison.OrdinalIgnoreCase) ||
               directoryName.Equals(
                   "ShaderCache",
                   StringComparison.OrdinalIgnoreCase) ||
               directoryName.Equals(
                   "CrashDumps",
                   StringComparison.OrdinalIgnoreCase) ||
               directoryName.Equals(
                   "Crashpad",
                   StringComparison.OrdinalIgnoreCase) ||
               directoryName.Equals(
                   "Logs",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldExcludeWholeProfileFile(
        string fileName)
    {
        if (fileName.Equals(
                "NTUSER.DAT",
                StringComparison.OrdinalIgnoreCase) ||
            fileName.Equals(
                "NTUSER.DAT.LOG1",
                StringComparison.OrdinalIgnoreCase) ||
            fileName.Equals(
                "NTUSER.DAT.LOG2",
                StringComparison.OrdinalIgnoreCase) ||
            fileName.Equals(
                "NTUSER.DAT{",
                StringComparison.OrdinalIgnoreCase) ||
            fileName.Equals(
                "UsrClass.dat",
                StringComparison.OrdinalIgnoreCase) ||
            fileName.Equals(
                "UsrClass.dat.LOG1",
                StringComparison.OrdinalIgnoreCase) ||
            fileName.Equals(
                "UsrClass.dat.LOG2",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var extension =
            Path.GetExtension(fileName);

        return extension.Equals(
                   ".tmp",
                   StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(
                   ".dmp",
                   StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(
                   ".log",
                   StringComparison.OrdinalIgnoreCase);
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






















