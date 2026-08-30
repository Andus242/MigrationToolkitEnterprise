using MTE.Core.Interfaces;
using MTE.Core.Models;

namespace MTE.Engine.Services;

public sealed class MigrationVerificationTestService
{
    private readonly FileMigrationService _fileMigrationService;
    private readonly IMigrationLogger _logger;

    public MigrationVerificationTestService(
        FileMigrationService fileMigrationService,
        IMigrationLogger logger)
    {
        _fileMigrationService = fileMigrationService;
        _logger = logger;
    }

        public async Task<VerificationResult> RunTestAsync(
        string destinationRoot,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var testDirectory = Path.Combine(
            Path.GetTempPath(),
            "MTE-Test");

        Directory.CreateDirectory(testDirectory);

        var sourceFile = Path.Combine(
            testDirectory,
            "MTE_Verification_Test.bin");

        var destinationDirectory = Path.Combine(
            destinationRoot,
            "MTE-Test");

        var destinationFile = Path.Combine(
            destinationDirectory,
            "MTE_Verification_Test.bin");

        try
        {
            const long testFileSize =
                100L * 1024 * 1024;

            _logger.Info(
                "Creating MTE verification test file (100 MB).");

            progress?.Report(0);

            await using (var stream = new FileStream(
                sourceFile,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 1024 * 1024,
                useAsync: true))
            {
                var buffer =
                    new byte[1024 * 1024];

                long bytesWritten = 0;

                while (bytesWritten < testFileSize)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var remaining =
                        testFileSize - bytesWritten;

                    var bytesToWrite =
                        (int)Math.Min(
                            buffer.Length,
                            remaining);

                    await stream.WriteAsync(
                        buffer.AsMemory(0, bytesToWrite),
                        cancellationToken);

                    bytesWritten += bytesToWrite;

                    var creationProgress =
                        (double)bytesWritten /
                        testFileSize *
                        10;

                    progress?.Report(
                        creationProgress);
                }

                await stream.FlushAsync(
                    cancellationToken);
            }

            _logger.Info(
                "Starting MTE verification test.");

            progress?.Report(10);

            var copyProgress =
                new Progress<double>(
                    value =>
                    {
                        var mappedProgress =
                            10 +
                            (value * 0.85);

                        progress?.Report(
                            Math.Min(
                                95,
                                mappedProgress));
                    });

            var result =
                await _fileMigrationService.CopyAndVerifyAsync(
                    sourceFile,
                    destinationFile,
                    copyProgress,
                    cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (result.Verified)
            {
                progress?.Report(100);
            }

            return result;
        }
        finally
        {
            try
            {
                if (File.Exists(sourceFile))
                {
                    File.Delete(sourceFile);
                }
            }
            catch
            {
                // Cleanup failure must not hide the verification result.
            }

            try
            {
                if (File.Exists(destinationFile))
                {
                    File.Delete(destinationFile);
                }

                if (Directory.Exists(destinationDirectory) &&
                    !Directory.EnumerateFileSystemEntries(destinationDirectory).Any())
                {
                    Directory.Delete(destinationDirectory);
                }
            }
            catch
            {
                // Cleanup failure must not hide the verification result.
            }
        }
    }
}


