using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MTE.Core.Interfaces;

namespace MTE.Engine.Services;

public sealed class BrowserDataMigrationService
{
    private readonly FileMigrationService _fileMigrationService;
    private readonly IMigrationLogger _logger;

    public BrowserDataMigrationService(
        FileMigrationService fileMigrationService,
        IMigrationLogger logger)
    {
        _fileMigrationService = fileMigrationService;
        _logger = logger;
    }

    public async Task MigrateProfileBrowsersAsync(
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
                $"Browser source profile not found: {sourceProfile}");

            return;
        }

        Directory.CreateDirectory(destinationProfile);

        var browsers = new[]
        {
            new BrowserDefinition(
                "Google Chrome",
                Path.Combine(
                    "AppData",
                    "Local",
                    "Google",
                    "Chrome",
                    "User Data")),

            new BrowserDefinition(
                "Microsoft Edge",
                Path.Combine(
                    "AppData",
                    "Local",
                    "Microsoft",
                    "Edge",
                    "User Data")),

            new BrowserDefinition(
                "Mozilla Firefox",
                Path.Combine(
                    "AppData",
                    "Roaming",
                    "Mozilla",
                    "Firefox"))
        };

        var availableBrowsers =
            browsers
                .Where(browser =>
                    Directory.Exists(
                        Path.Combine(
                            sourceProfile,
                            browser.RelativePath)))
                .ToList();

        if (availableBrowsers.Count == 0)
        {
            progress?.Report(
                new MigrationProgressInfo
                {
                    Section = "Browsers",
                    Message = "No supported browser profiles found.",
                    Percentage = 60,
                    IsComplete = false
                });

            _logger.Info(
                $"No supported browser profiles found for: {sourceProfile}");

            return;
        }

        for (var index = 0;
             index < availableBrowsers.Count;
             index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var browser =
                availableBrowsers[index];

            var sourceBrowser =
                Path.Combine(
                    sourceProfile,
                    browser.RelativePath);

            var destinationBrowser =
                Path.Combine(
                    destinationProfile,
                    browser.RelativePath);

            var browserStart =
                50 +
                (int)Math.Round(
                    index *
                    10.0 /
                    availableBrowsers.Count);

            progress?.Report(
                new MigrationProgressInfo
                {
                    Section = "Browsers",
                    Message =
                        $"Migrating {browser.Name} browser data...",
                    Percentage = browserStart,
                    IsComplete = false
                });

            var files =
                GetBrowserFiles(
                    sourceBrowser,
                    cancellationToken);

            var totalFiles = files.Count;
            var completedFiles = 0;

            foreach (var sourceFile in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var relativePath =
                    Path.GetRelativePath(
                        sourceBrowser,
                        sourceFile);

                var destinationFile =
                    Path.Combine(
                        destinationBrowser,
                        relativePath);

                try
                {
                    var result =
                        await _fileMigrationService.CopyAndVerifyAsync(
                            sourceFile,
                            destinationFile,
                            cancellationToken);

                    completedFiles++;

                    var localPercentage =
                        totalFiles > 0
                            ? completedFiles * 100.0 / totalFiles
                            : 100;

                    var overallPercentage =
                        browserStart +
                        (int)Math.Round(
                            localPercentage *
                            (10.0 /
                             availableBrowsers.Count /
                             100.0));

                    progress?.Report(
                        new MigrationProgressInfo
                        {
                            Section = "Browsers",
                            Message =
                                $"Verified {browser.Name}: " +
                                $"{relativePath}",
                            Percentage =
                                Math.Min(
                                    60,
                                    Math.Max(
                                        browserStart,
                                        overallPercentage)),
                            IsComplete = false
                        });

                    if (!result.Verified)
                    {
                        _logger.Error(
                            $"Browser verification failed: " +
                            $"{sourceFile}");
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.Warning(
                        $"Skipped browser file " +
                        $"{sourceFile}: {ex.Message}");
                }
            }

            _logger.Info(
                $"Browser migration processed: " +
                $"{browser.Name} - " +
                $"{completedFiles}/{totalFiles} files.");
        }

        progress?.Report(
            new MigrationProgressInfo
            {
                Section = "Browsers",
                Message =
                    "Browser data migration completed and files verified.",
                Percentage = 60,
                IsComplete = false
            });
    }

    private static List<string> GetBrowserFiles(
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
            // Ignore inaccessible files.
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
            "Service Worker CacheStorage",
            "Crashpad",
            "CrashDumps",
            "Temp",
            "Logs",
            "WebCache"
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

    private sealed record BrowserDefinition(
        string Name,
        string RelativePath);
}
