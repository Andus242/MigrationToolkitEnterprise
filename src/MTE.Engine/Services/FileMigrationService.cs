using MTE.Core.Interfaces;
using MTE.Core.Models;

namespace MTE.Engine.Services;

public sealed class FileMigrationService
{
    private readonly IDataVerificationService _verificationService;
    private readonly IMigrationLogger _logger;

    public FileMigrationService(
        IDataVerificationService verificationService,
        IMigrationLogger logger)
    {
        _verificationService = verificationService;
        _logger = logger;
    }

    // Backward-compatible overload for existing migration services.
    public Task<VerificationResult> CopyAndVerifyAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        return CopyAndVerifyAsync(
            sourcePath,
            destinationPath,
            null,
            cancellationToken);
    }
    public async Task<VerificationResult> CopyAndVerifyAsync(
        string sourcePath,
        string destinationPath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        string? temporaryPath = null;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report(0);

            var destinationDirectory =
                Path.GetDirectoryName(destinationPath);

            if (!string.IsNullOrWhiteSpace(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            var fileInfo =
                new FileInfo(sourcePath);

            var totalBytes =
                fileInfo.Length;

            long bytesCopied = 0;
            int lastReportedPercentage = -1;

            _logger.Info(
                $"Copying: {sourcePath} -> {destinationPath}");

            temporaryPath =
                Path.Combine(
                    destinationDirectory!,
                    "." +
                    Path.GetFileName(destinationPath) +
                    ".mte-temp-" +
                    Guid.NewGuid().ToString("N"));

            await using (var sourceStream = new FileStream(
                sourcePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 1024 * 64,
                useAsync: true))
            {
                await using var destinationStream =
                    new FileStream(
                        temporaryPath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None,
                        bufferSize: 1024 * 64,
                        useAsync: true);

                var buffer =
                    new byte[1024 * 64];

                int bytesRead;

                while ((bytesRead = await sourceStream.ReadAsync(
                    buffer.AsMemory(0, buffer.Length),
                    cancellationToken)) > 0)
                {
                    await destinationStream.WriteAsync(
                        buffer.AsMemory(0, bytesRead),
                        cancellationToken);

                    bytesCopied += bytesRead;

                    var percentage =
                        totalBytes == 0
                            ? 100
                            : (double)bytesCopied / totalBytes * 100;

                    var reportedPercentage =
                        Math.Clamp((int)percentage, 0, 100);

                    if (reportedPercentage != lastReportedPercentage)
                    {
                        lastReportedPercentage = reportedPercentage;
                        progress?.Report(reportedPercentage);
                    }
                }

                await destinationStream.FlushAsync(
                    cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var result =
                await _verificationService.VerifyFileAsync(
                    sourcePath,
                    temporaryPath,
                    cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (!result.Verified)
            {
                _logger.Error(
                    $"Verification failed: {destinationPath}");

                return new VerificationResult
                {
                    SourcePath = sourcePath,
                    DestinationPath = destinationPath,
                    FileSize = result.FileSize,
                    SourceHash = result.SourceHash,
                    DestinationHash = result.DestinationHash,
                    Exists = result.Exists,
                    HashMatches = result.HashMatches,
                    Timestamp = result.Timestamp
                };
            }

            cancellationToken.ThrowIfCancellationRequested();

            File.Move(
                temporaryPath,
                destinationPath,
                true);

            temporaryPath = null;

            progress?.Report(100);

            _logger.Success(
                $"Verified: {destinationPath}");

            return new VerificationResult
            {
                SourcePath = sourcePath,
                DestinationPath = destinationPath,
                FileSize = result.FileSize,
                SourceHash = result.SourceHash,
                DestinationHash = result.DestinationHash,
                Exists = true,
                HashMatches = true,
                Timestamp = DateTime.Now
            };
        }
        catch (OperationCanceledException)
        {
            _logger.Warning(
                $"Copy cancelled: {sourcePath}");

            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(
                $"Copy failed: {sourcePath} - {ex.Message}");

            throw;
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(temporaryPath))
            {
                try
                {
                    if (File.Exists(temporaryPath))
                    {
                        File.Delete(temporaryPath);
                    }
                }
                catch (Exception cleanupException)
                {
                    _logger.Warning(
                        $"Could not clean up temporary file " +
                        $"{temporaryPath}: " +
                        $"{cleanupException.Message}");
                }
            }
        }
    }
}


