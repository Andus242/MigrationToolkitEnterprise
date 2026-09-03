using MTE.Core.Interfaces;

namespace MTE.Engine.Sections;

public sealed class UserProfileSection : IMigrationSection
{
    private readonly IUserProfileDiscoveryService
        _discoveryService;

    public UserProfileSection(
        IUserProfileDiscoveryService discoveryService)
    {
        _discoveryService = discoveryService;
    }

    public string Name => "User Profiles";

    public async Task ExecuteAsync(
        IProgress<MigrationProgressInfo> progress,
        CancellationToken cancellationToken)
    {
        progress.Report(
            new MigrationProgressInfo
            {
                Section = Name,
                Message = "Discovering user profiles...",
                Percentage = 25,
                IsComplete = false
            });

        var profiles =
            await _discoveryService.DiscoverAsync(
                cancellationToken);

        var userProfiles =
            profiles
                .Where(profile =>
                    !profile.IsSystemProfile)
                .ToList();

        progress.Report(
            new MigrationProgressInfo
            {
                Section = Name,
                Message =
                    $"Found {userProfiles.Count} user profile(s).",
                Percentage = 28,
                IsComplete = false
            });

        await Task.Delay(
            300,
            cancellationToken);

        foreach (var profile in userProfiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sizeMb =
                profile.SizeBytes /
                1024d /
                1024d;

            progress.Report(
                new MigrationProgressInfo
                {
                    Section = Name,
                    Message =
                        $"{profile.UserName} - " +
                        $"{sizeMb:N1} MB - " +
                        $"{profile.ProfilePath}",
                    Percentage = 30,
                    IsComplete = false
                });

            await Task.Delay(
                150,
                cancellationToken);
        }

        progress.Report(
            new MigrationProgressInfo
            {
                Section = Name,
                Message =
                    "User profile discovery completed.",
                Percentage = 35,
                IsComplete = false
            });
    }
}