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
        CancellationToken cancellationToken = default)
    {
        var testDirectory = Path.Combine(
            Path.GetTempPath(),
            "MTE-Test");

        Directory.CreateDirectory(testDirectory);

        var sourceFile = Path.Combine(
            testDirectory,
            "MTE_Verification_Test.txt");

        var destinationDirectory = Path.Combine(
            destinationRoot,
            "MTE-Test");

        var destinationFile = Path.Combine(
            destinationDirectory,
            "MTE_Verification_Test.txt");

        var testContent =
            "Migration Toolkit Enterprise verification test." +
            Environment.NewLine +
            $"Created: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

        await File.WriteAllTextAsync(
            sourceFile,
            testContent,
            cancellationToken);

        _logger.Info(
            "Starting MTE verification test.");

        var result =
            await _fileMigrationService.CopyAndVerifyAsync(
                sourceFile,
                destinationFile,
                cancellationToken);

        return result;
    }
}