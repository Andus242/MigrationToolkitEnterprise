using MTE.Core.Interfaces;
using MTE.Engine.Sections.Discovery;

namespace MTE.Engine.Engine;

public sealed class MigrationEngine : IMigrationEngine
{
    private readonly IMigrationLogger _logger;

    public MigrationEngine(IMigrationLogger logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(
        IProgress<MigrationProgressInfo> progress,
        CancellationToken cancellationToken)
    {
        _logger.Info("Migration engine started.");

        try
        {
            // ============================================================
            // 1. SYSTEM DISCOVERY
            // ============================================================

            Report(
                progress,
                "System Discovery",
                "Starting system discovery...",
                0);

            var discovery = new SystemDiscoverySection();

            await discovery.ExecuteAsync(
                new Progress<MigrationProgressInfo>(p =>
                {
                    // Map System Discovery progress to the overall engine.
                    // Discovery occupies 0-20% of the complete migration.
                    var overallPercentage =
                        Math.Min(
                            20,
                            (int)Math.Round(p.Percentage * 0.20));

                    progress.Report(new MigrationProgressInfo
                    {
                        Section = p.Section,
                        Message = p.Message,
                        Percentage = overallPercentage,
                        IsComplete = p.IsComplete
                    });
                }),
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            // ============================================================
            // 2. PREPARATION
            // ============================================================

            Report(
                progress,
                "Preparation",
                "Preparing migration...",
                25);

            await Task.Delay(500, cancellationToken);

            // ============================================================
            // 3. USER PROFILES
            // ============================================================

            Report(
                progress,
                "User Profiles",
                "Preparing user profile migration...",
                40);

            await Task.Delay(500, cancellationToken);

            // ============================================================
            // 4. DOCUMENTS
            // ============================================================

            Report(
                progress,
                "Documents",
                "Preparing document migration...",
                55);

            await Task.Delay(500, cancellationToken);

            // ============================================================
            // 5. APPLICATIONS
            // ============================================================

            Report(
                progress,
                "Applications",
                "Preparing application settings...",
                70);

            await Task.Delay(500, cancellationToken);

            // ============================================================
            // 6. NETWORK
            // ============================================================

            Report(
                progress,
                "Network",
                "Preparing network configuration...",
                80);

            await Task.Delay(500, cancellationToken);

            // ============================================================
            // 7. FINALIZATION
            // ============================================================

            Report(
                progress,
                "Finalization",
                "Finalizing migration...",
                90);

            await Task.Delay(500, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            // ============================================================
            // 8. COMPLETE
            // ============================================================

            Report(
                progress,
                "Complete",
                "Migration completed successfully.",
                100,
                true);

            _logger.Success(
                "Migration engine completed successfully.");
        }
        catch (OperationCanceledException)
        {
            _logger.Warning(
                "Migration was cancelled.");

            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(
                $"Migration failed: {ex}");

            throw;
        }
    }

    private static void Report(
        IProgress<MigrationProgressInfo> progress,
        string section,
        string message,
        int percentage,
        bool isComplete = false)
    {
        progress.Report(
            new MigrationProgressInfo
            {
                Section = section,
                Message = message,
                Percentage = percentage,
                IsComplete = isComplete
            });
    }
}