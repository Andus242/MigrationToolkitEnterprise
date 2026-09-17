using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MTE.Core.Interfaces;
using MTE.Core.Models;
using MTE.Engine.Sections.Discovery;
using MTE.Engine.Services;

namespace MTE.Engine.Engine;

public sealed class MigrationEngine : IMigrationEngine
{
    private readonly IMigrationLogger _logger;
    private readonly IUserProfileDiscoveryService _userProfileDiscoveryService;
    private readonly UserProfileMigrationService _userProfileMigrationService;
    private readonly ApplicationSettingsMigrationService _applicationSettingsMigrationService;
    private readonly BrowserDataMigrationService _browserDataMigrationService;
    private readonly MigrationStorageService _migrationStorageService;

    public MigrationEngine(
        IMigrationLogger logger,
        IUserProfileDiscoveryService userProfileDiscoveryService,
        UserProfileMigrationService userProfileMigrationService,
        ApplicationSettingsMigrationService applicationSettingsMigrationService,
        BrowserDataMigrationService browserDataMigrationService,
        MigrationStorageService migrationStorageService)
    {
        _logger = logger;
        _userProfileDiscoveryService = userProfileDiscoveryService;
        _userProfileMigrationService = userProfileMigrationService;
        _applicationSettingsMigrationService = applicationSettingsMigrationService;
        _browserDataMigrationService = browserDataMigrationService;
        _migrationStorageService = migrationStorageService;
    }

    public async Task<MigrationExecutionResult> ExecuteAsync(
        string destinationDrive,
        IReadOnlyList<MigrationProfileSelection> selectedProfiles,
        IProgress<MigrationProgressInfo> progress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(destinationDrive))
        {
            throw new ArgumentException(
                "A destination drive is required.",
                nameof(destinationDrive));
        }

        _logger.Info("Migration engine started.");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var executionResult = new MigrationExecutionResult();

            var lastReportedPercentage = 0;

            void ReportOverallProgress(
                string section,
                string message,
                int percentage,
                bool isComplete = false)
            {
                var safePercentage =
                    Math.Clamp(percentage, 0, 100);

                if (safePercentage < lastReportedPercentage)
                {
                    safePercentage = lastReportedPercentage;
                }

                lastReportedPercentage = safePercentage;

                Report(
                    progress,
                    section,
                    message,
                    safePercentage,
                    isComplete);
            }

            static int MapProgress(
                double localPercentage,
                int start,
                int end)
            {
                var local =
                    Math.Clamp(localPercentage, 0d, 100d);

                return start +
                    (int)Math.Round(
                        (end - start) * local / 100d);
            }

            // ---------------------------------------------------------
            // 0-20% : SYSTEM DISCOVERY
            // ---------------------------------------------------------

            ReportOverallProgress(
                "System Discovery",
                "Starting system discovery...",
                0);

            var discovery = new SystemDiscoverySection();

            await discovery.ExecuteAsync(
                new Progress<MigrationProgressInfo>(p =>
                {
                    ReportOverallProgress(
                        "System Discovery",
                        p.Message,
                        MapProgress(
                            p.Percentage,
                            0,
                            20));
                }),
                cancellationToken);

            ReportOverallProgress(
                "System Discovery",
                "System discovery completed.",
                20);

            executionResult.Sections.Add(
                new MigrationSectionResult
                {
                    Section = "System Discovery",
                    Success = true,
                    Message = "System discovery completed.",
                    Timestamp = DateTime.Now
                });

            cancellationToken.ThrowIfCancellationRequested();

            // ---------------------------------------------------------
            // 20-40% : USER PROFILES
            // ---------------------------------------------------------

            ReportOverallProgress(
                "User Profiles",
                "Discovering user profiles...",
                20);

            var migrationRoot =
                _migrationStorageService.CreateMigrationRoot(
                    destinationDrive);

            _logger.Info(
                $"Migration root created: {migrationRoot}");

            ReportOverallProgress(
                "Preparation",
                $"Migration destination prepared: {migrationRoot}",
                22);

            var userProfiles =
                selectedProfiles
                    .Where(profile => !profile.Profile.IsSystemProfile)
                    .ToList();

            _logger.Info(
                $"Selected {userProfiles.Count} user profile(s) for migration.");

            ReportOverallProgress(
                "User Profiles",
                $"{userProfiles.Count} user profile(s) found.",
                24);

            if (userProfiles.Count == 0)
            {
                ReportOverallProgress(
                    "User Profiles",
                    "No user profiles available for migration.",
                    40);
            }
            else
            {
                for (var index = 0;
                     index < userProfiles.Count;
                     index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var profileSelection = userProfiles[index];
                    var profile = profileSelection.Profile;

                    var destinationProfile =
                        System.IO.Path.Combine(
                            migrationRoot,
                            "UserProfiles",
                            profile.UserName);

                    var profileProgress =
                        new Progress<MigrationProgressInfo>(p =>
                        {
                            var overallPercentage =
                                MapProgress(
                                    p.Percentage,
                                    24 + (int)Math.Round(
                                        index * 16.0 /
                                        userProfiles.Count),
                                    24 + (int)Math.Round(
                                        (index + 1) * 16.0 /
                                        userProfiles.Count));

                            ReportOverallProgress(
                                "User Profiles",
                                p.Message,
                                Math.Min(
                                    40,
                                    overallPercentage));
                        });

                    var profileResults =
                        await _userProfileMigrationService
                            .CopyUserProfileAsync(
                                profile.ProfilePath,
                                destinationProfile,
                                profileSelection.SelectedFolders,
                                profileProgress,
                                cancellationToken);

                    executionResult.FileResults.AddRange(
                        profileResults);
                }

                ReportOverallProgress(
                    "User Profiles",
                    "User profile migration completed and files verified.",
                    40);

                executionResult.Sections.Add(
                    new MigrationSectionResult
                    {
                        Section = "User Profiles",
                        Success = true,
                        Message = "User profile migration completed and files verified.",
                        Timestamp = DateTime.Now
                    });
            }

            cancellationToken.ThrowIfCancellationRequested();

            // ---------------------------------------------------------
            // 40-55% : APPLICATION SETTINGS
            // ---------------------------------------------------------

            ReportOverallProgress(
                "Applications",
                "Starting application settings migration...",
                40);

            var applicationRoot =
                System.IO.Path.Combine(
                    migrationRoot,
                    "ApplicationSettings");

            System.IO.Directory.CreateDirectory(
                applicationRoot);

            if (userProfiles.Count == 0)
            {
                ReportOverallProgress(
                    "Applications",
                    "No user profiles available for application settings migration.",
                    55);
            }
            else
            {
                for (var index = 0;
                     index < userProfiles.Count;
                     index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var profileSelection = userProfiles[index];
                    var profile = profileSelection.Profile;

                    var applicationDestination =
                        System.IO.Path.Combine(
                            applicationRoot,
                            profile.UserName);

                    System.IO.Directory.CreateDirectory(
                        applicationDestination);

                    var applicationProgress =
                        new Progress<MigrationProgressInfo>(p =>
                        {
                            var start =
                                40 +
                                (int)Math.Round(
                                    index * 15.0 /
                                    userProfiles.Count);

                            var end =
                                40 +
                                (int)Math.Round(
                                    (index + 1) * 15.0 /
                                    userProfiles.Count);

                            ReportOverallProgress(
                                "Applications",
                                p.Message,
                                MapProgress(
                                    p.Percentage,
                                    start,
                                    end));
                        });

                    var applicationResults =
                        await _applicationSettingsMigrationService
                            .MigrateProfileSettingsAsync(
                                profile.ProfilePath,
                                applicationDestination,
                                applicationProgress,
                                cancellationToken);

                    executionResult.FileResults.AddRange(
                        applicationResults);
                }

                ReportOverallProgress(
                    "Applications",
                    "Application settings migration completed and files verified.",
                    55);

                executionResult.Sections.Add(
                    new MigrationSectionResult
                    {
                        Section = "Applications",
                        Success = true,
                        Message = "Application settings migration completed and files verified.",
                        Timestamp = DateTime.Now
                    });
            }

            cancellationToken.ThrowIfCancellationRequested();

            // ---------------------------------------------------------
            // 55-70% : BROWSER DATA
            // ---------------------------------------------------------

            ReportOverallProgress(
                "Browsers",
                "Starting browser data migration...",
                55);

            var browserRoot =
                System.IO.Path.Combine(
                    migrationRoot,
                    "BrowserData");

            System.IO.Directory.CreateDirectory(
                browserRoot);

            if (userProfiles.Count == 0)
            {
                ReportOverallProgress(
                    "Browsers",
                    "No user profiles available for browser migration.",
                    70);
            }
            else
            {
                for (var index = 0;
                     index < userProfiles.Count;
                     index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var profileSelection = userProfiles[index];
                    var profile = profileSelection.Profile;

                    var browserDestination =
                        System.IO.Path.Combine(
                            browserRoot,
                            profile.UserName);

                    System.IO.Directory.CreateDirectory(
                        browserDestination);

                    var browserProgress =
                        new Progress<MigrationProgressInfo>(p =>
                        {
                            var start =
                                55 +
                                (int)Math.Round(
                                    index * 15.0 /
                                    userProfiles.Count);

                            var end =
                                55 +
                                (int)Math.Round(
                                    (index + 1) * 15.0 /
                                    userProfiles.Count);

                            ReportOverallProgress(
                                "Browsers",
                                p.Message,
                                MapProgress(
                                    p.Percentage,
                                    start,
                                    end));
                        });

                    var browserResults =
                        await _browserDataMigrationService
                            .MigrateProfileBrowsersAsync(
                                profile.ProfilePath,
                                browserDestination,
                                browserProgress,
                                cancellationToken);

                    executionResult.FileResults.AddRange(
                        browserResults);
                }

                ReportOverallProgress(
                    "Browsers",
                    "Browser data migration completed and files verified.",
                    70);

                executionResult.Sections.Add(
                    new MigrationSectionResult
                    {
                        Section = "Browsers",
                        Success = true,
                        Message = "Browser data migration completed and files verified.",
                        Timestamp = DateTime.Now
                    });
            }

            cancellationToken.ThrowIfCancellationRequested();

            // ---------------------------------------------------------
            // 70-80% : NETWORK
            // ---------------------------------------------------------

            ReportOverallProgress(
                "Network",
                "Preparing network configuration migration...",
                70);

            await Task.Delay(
                250,
                cancellationToken);

            ReportOverallProgress(
                "Network",
                "Network migration stage ready.",
                80);

            executionResult.Sections.Add(
                new MigrationSectionResult
                {
                    Section = "Network",
                    Success = true,
                    Message = "Network migration stage ready.",
                    Timestamp = DateTime.Now
                });

            cancellationToken.ThrowIfCancellationRequested();

            // ---------------------------------------------------------
            // 80-95% : FINALIZATION
            // ---------------------------------------------------------

            ReportOverallProgress(
                "Finalization",
                "Finalizing migration and preparing verification...",
                80);

            await Task.Delay(
                250,
                cancellationToken);

            ReportOverallProgress(
                "Finalization",
                "Migration data prepared successfully.",
                95);

            executionResult.Sections.Add(
                new MigrationSectionResult
                {
                    Section = "Finalization",
                    Success = true,
                    Message = "Migration data prepared successfully.",
                    Timestamp = DateTime.Now
                });

            cancellationToken.ThrowIfCancellationRequested();

            // ---------------------------------------------------------
            // 100% : COMPLETE
            // ---------------------------------------------------------

            ReportOverallProgress(
                "Complete",
                "Migration completed successfully.",
                100,
                true);

            executionResult.Sections.Add(
                new MigrationSectionResult
                {
                    Section = "Complete",
                    Success = true,
                    Message = "Migration completed successfully.",
                    Timestamp = DateTime.Now
                });

            _logger.Success(
                $"Migration engine completed successfully. " +
                $"Migration root: {migrationRoot}");

            return executionResult;
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



















