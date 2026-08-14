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

    public async Task<VerificationResult> CopyAndVerifyAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var destinationDirectory =
                Path.GetDirectoryName(destinationPath);

            if (!string.IsNullOrWhiteSpace(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            _logger.Info(
                $"Copying: {sourcePath} -> {destinationPath}");

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
                        destinationPath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None,
                        bufferSize: 1024 * 64,
                        useAsync: true);

                await sourceStream.CopyToAsync(
                    destinationStream,
                    cancellationToken);
            }

            var result =
                await _verificationService.VerifyFileAsync(
                    sourcePath,
                    destinationPath,
                    cancellationToken);

            if (result.Verified)
            {
                _logger.Success(
                    $"Verified: {destinationPath}");
            }
            else
            {
                _logger.Error(
                    $"Verification failed: {destinationPath}");
            }

            return result;
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
    }
}