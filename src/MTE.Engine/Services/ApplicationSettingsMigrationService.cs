using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MTE.Core.Interfaces;

namespace MTE.Engine.Services;

public sealed class ApplicationSettingsMigrationService
{
    private readonly FileMigrationService _fileMigrationService;
    private readonly IMigrationLogger _logger;

    public ApplicationSettingsMigrationService(
        FileMigrationService fileMigrationService,
        IMigrationLogger logger)
    {
        _fileMigrationService = fileMigrationService;
        _logger = logger;
    }

    public async Task MigrateProfileSettingsAsync(
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
                $"Application settings source profile not found: {sourceProfile}");

            return;
        }

        Directory.CreateDirectory(destinationProfile);

        var appDataFolders = new[]
        {
            "Roaming",
            "Local"
        };

        for (var index = 0; index < appDataFolders.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var folderName = appDataFolders[index];

            var sourceFolder =
                Path.Combine(
                    sourceProfile,
                    "AppData",
                    folderName);

            var destinationFolder =
                Path.Combine(
                    destinationProfile,
                    "AppData",
                    folderName);

            if (!Directory.Exists(sourceFolder))
            {
                _logger.Info(
                    $"Application settings folder not found: {sourceFolder}");

                continue;
            }

            progress?.Report(
                new MigrationProgressInfo
                {
                    Section = "Applications",
                    Message =
                        $"Scanning application settings: {folderName}...",
                    Percentage = 36 + index * 2,
                    IsComplete = false
                });

            var files =
                GetFilesExcludingCache(
                    sourceFolder,
                    cancellationToken);

            var totalFiles = files.Count;
            var completedFiles = 0;

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
                            cancellationToken);

                    completedFiles++;

                    var percentage =
                        totalFiles > 0
                            ? completedFiles * 100.0 / totalFiles
                            : 100;

                    progress?.Report(
                        new MigrationProgressInfo
                        {
                            Section = "Applications",
                            Message =
                                $"Migrating application settings: " +
                                $"{folderName}\\{relativePath}",
                            Percentage =
                                Math.Min(
                                    50,
                                    38 +
                                    (int)Math.Round(
                                        percentage * 0.12)),
                            IsComplete = false
                        });

                    if (!result.Verified)
                    {
                        _logger.Error(
                            $"Application setting verification failed: {sourceFile}");
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.Warning(
                        $"Skipped application setting " +
                        $"{sourceFile}: {ex.Message}");
                }
            }

            _logger.Info(
                $"Application settings processed: " +
                $"{folderName} - {completedFiles}/{totalFiles} files.");
        }

        progress?.Report(
            new MigrationProgressInfo
            {
                Section = "Applications",
                Message =
                    "Application settings migration completed.",
                Percentage = 50,
                IsComplete = false
            });
    }

    private static List<string> GetFilesExcludingCache(
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

        if (ShouldExcludeDirectory(info.Name))
            return;

        try
        {
            foreach (var file in info.EnumerateFiles())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (ShouldExcludeFile(file))
                    continue;

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

                if (ShouldExcludeDirectory(
                        subdirectory.Name))
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
            // Ignore inaccessible directories.
        }
    }

    private static bool ShouldExcludeDirectory(
        string directoryName)
    {
        var excludedDirectories = new[]
        {
            "Cache",
            "Caches",
            "Code Cache",
            "GPUCache",
            "ShaderCache",
            "Temp",
            "Temporary Internet Files",
            "INetCache",
            "WebCache",
            "CrashDumps",
            "Logs"
        };

        return excludedDirectories.Any(
            excluded =>
                directoryName.Equals(
                    excluded,
                    StringComparison.OrdinalIgnoreCase));
    }

    private static bool ShouldExcludeFile(
        FileInfo file)
    {
        var extension = file.Extension;

        return extension.Equals(
                   ".tmp",
                   StringComparison.OrdinalIgnoreCase)
               ||
               extension.Equals(
                   ".log",
                   StringComparison.OrdinalIgnoreCase)
               ||
               extension.Equals(
                   ".dmp",
                   StringComparison.OrdinalIgnoreCase);
    }
}
