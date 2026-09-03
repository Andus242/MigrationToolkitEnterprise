using System.IO;
using MTE.Core.Interfaces;

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
                Directory.CreateDirectory(destinationFolder);

            try
            {
                await CopyFileAsync(
                    sourceFile,
                    destinationFile,
                    cancellationToken);

                copiedFiles++;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                skippedFiles++;

                progress?.Report(new MigrationProgressInfo
                {
                    Section = "Documents",
                    Message =
                        $"Skipped: {Path.GetFileName(sourceFile)} - {ex.Message}",
                    Percentage =
                        CalculatePercentage(
                            copiedFiles,
                            skippedFiles,
                            totalFiles),
                    IsComplete = false
                });

                continue;
            }

            progress?.Report(new MigrationProgressInfo
            {
                Section = "Documents",
                Message =
                    $"Copying {copiedFiles} of {totalFiles}: {Path.GetFileName(sourceFile)}",
                Percentage =
                    CalculatePercentage(
                        copiedFiles,
                        skippedFiles,
                        totalFiles),
                IsComplete =
                    copiedFiles + skippedFiles >= totalFiles
            });
        }

        progress?.Report(new MigrationProgressInfo
        {
            Section = "Documents",
            Message =
                $"Documents complete. Copied: {copiedFiles}, Skipped: {skippedFiles}. OneDrive excluded.",
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

    private static int CalculatePercentage(
        int copiedFiles,
        int skippedFiles,
        int totalFiles)
    {
        if (totalFiles <= 0)
            return 100;

        return Math.Min(
            100,
            (int)Math.Round(
                (copiedFiles + skippedFiles) * 100.0 /
                totalFiles));
    }

    private static async Task CopyFileAsync(
        string sourceFile,
        string destinationFile,
        CancellationToken cancellationToken)
    {
        const int bufferSize = 1024 * 1024;

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

        await source.CopyToAsync(
            destination,
            bufferSize,
            cancellationToken);
    }
}