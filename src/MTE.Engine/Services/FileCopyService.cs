using System.IO;
using MTE.Core.Interfaces;
using MTE.Core.Models;

namespace MTE.Engine.Services;

public sealed class FileCopyService
{
    public async Task CopyDirectoryAsync(
        string sourceDirectory,
        string destinationDirectory,
        IProgress<MigrationProgressInfo>? progress,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            progress?.Report(new MigrationProgressInfo
            {
                Section = "Documents",
                Message = "Source folder does not exist. Skipping.",
                Percentage = 0,
                IsComplete = true
            });

            return;
        }

        Directory.CreateDirectory(destinationDirectory);

        var files = GetFilesExcludingOneDrive(
            sourceDirectory,
            cancellationToken);

        var totalFiles = files.Count;

        if (totalFiles == 0)
        {
            progress?.Report(new MigrationProgressInfo
            {
                Section = "Documents",
                Message = "No files found after exclusions.",
                Percentage = 100,
                IsComplete = true
            });

            return;
        }

        var copiedFiles = 0;
        var skippedFiles = 0;
        var lastReportedProgress = 0;

        foreach (var sourceFile in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath =
                Path.GetRelativePath(
                    sourceDirectory,
                    sourceFile);

            var destinationFile =
                Path.Combine(
                    destinationDirectory,
                    relativePath);

            var destinationFolder =
                Path.GetDirectoryName(destinationFile);

            if (!string.IsNullOrEmpty(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
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
                            ((copiedFiles +
                              safeFilePercentage / 100d +
                              skippedFiles) /
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
                        else
                        {
                            lastReportedProgress =
                                reportedProgress;
                        }

                        progress?.Report(new MigrationProgressInfo
                        {
                            Section = "Documents",
                            Message =
                                $"Copying: {relativePath}",
                            Percentage = reportedProgress,
                            IsComplete = false
                        });
                    });

                await CopyFileAsync(
                    sourceFile,
                    destinationFile,
                    fileProgress,
                    cancellationToken);

                copiedFiles++;

                var completedPercentage =
                    Math.Clamp(
                        (int)Math.Round(
                            (copiedFiles + skippedFiles) *
                            100.0 /
                            totalFiles),
                        0,
                        99);

                if (completedPercentage < lastReportedProgress)
                {
                    completedPercentage =
                        lastReportedProgress;
                }
                else
                {
                    lastReportedProgress =
                        completedPercentage;
                }

                progress?.Report(new MigrationProgressInfo
                {
                    Section = "Documents",
                    Message =
                        $"Copied {copiedFiles:N0} of {totalFiles:N0}: {currentFileName}",
                    Percentage = completedPercentage,
                    IsComplete = false
                });
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                skippedFiles++;

                var failedPercentage =
                    Math.Clamp(
                        (int)Math.Round(
                            (copiedFiles + skippedFiles) *
                            100.0 /
                            totalFiles),
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

                progress?.Report(new MigrationProgressInfo
                {
                    Section = "Documents",
                    Message =
                        $"Skipped: {Path.GetFileName(sourceFile)} - {ex.Message}",
                    Percentage = failedPercentage,
                    IsComplete = false
                });

                continue;
            }
        }

        progress?.Report(new MigrationProgressInfo
        {
            Section = "Documents",
            Message =
                $"Documents complete. Copied: {copiedFiles:N0}, Skipped: {skippedFiles:N0}. OneDrive excluded.",
            Percentage = 100,
            IsComplete = true
        });
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

        DirectoryInfo directoryInfo;

        try
        {
            directoryInfo = new DirectoryInfo(directory);
        }
        catch
        {
            return;
        }

        if (IsOneDriveDirectory(directoryInfo))
            return;

        IEnumerable<FileInfo> directoryFiles;

        try
        {
            directoryFiles = directoryInfo.EnumerateFiles();
        }
        catch
        {
            return;
        }

        foreach (var file in directoryFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            files.Add(file.FullName);
        }

        IEnumerable<DirectoryInfo> subdirectories;

        try
        {
            subdirectories = directoryInfo.EnumerateDirectories();
        }
        catch
        {
            return;
        }

        foreach (var subdirectory in subdirectories)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsOneDriveDirectory(subdirectory))
                continue;

            ScanDirectory(
                subdirectory.FullName,
                files,
                cancellationToken);
        }
    }

    private static bool IsOneDriveDirectory(
        DirectoryInfo directory)
    {
        return
            directory.Name.Equals(
                "OneDrive",
                StringComparison.OrdinalIgnoreCase)
            ||
            directory.Name.StartsWith(
                "OneDrive - ",
                StringComparison.OrdinalIgnoreCase);
    }

    private static async Task CopyFileAsync(
        string sourceFile,
        string destinationFile,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        const int bufferSize = 1024 * 1024;

        var fileInfo =
            new FileInfo(sourceFile);

        var totalBytes =
            fileInfo.Length;

        long bytesCopied = 0;

        progress?.Report(0);

        await using var source =
            new FileStream(
                sourceFile,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize,
                FileOptions.Asynchronous |
                FileOptions.SequentialScan);

        await using var destination =
            new FileStream(
                destinationFile,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize,
                FileOptions.Asynchronous |
                FileOptions.SequentialScan);

        var buffer =
            new byte[bufferSize];

        int bytesRead;

        while ((bytesRead =
            await source.ReadAsync(
                buffer.AsMemory(
                    0,
                    buffer.Length),
                cancellationToken)) > 0)
        {
            await destination.WriteAsync(
                buffer.AsMemory(
                    0,
                    bytesRead),
                cancellationToken);

            bytesCopied += bytesRead;

            var percentage =
                totalBytes == 0
                    ? 100d
                    : (double)bytesCopied /
                      totalBytes *
                      100d;

            progress?.Report(
                Math.Min(
                    100d,
                    percentage));
        }

        await destination.FlushAsync(
            cancellationToken);

        progress?.Report(100);
    }
}
