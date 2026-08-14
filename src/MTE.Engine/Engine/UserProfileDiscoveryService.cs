using MTE.Core.Interfaces;
using MTE.Core.Models;

namespace MTE.Engine.Engine;

public sealed class UserProfileDiscoveryService
    : IUserProfileDiscoveryService
{
    public Task<IReadOnlyList<UserProfileInformation>> DiscoverAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var profiles = new List<UserProfileInformation>();

        var usersDirectory = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile),
            "..");

        var resolvedUsersDirectory =
            Path.GetFullPath(usersDirectory);

        if (!Directory.Exists(resolvedUsersDirectory))
        {
            return Task.FromResult<
                IReadOnlyList<UserProfileInformation>>(
                profiles);
        }

        var currentUser =
            Environment.UserName;

        foreach (var directory in
                 Directory.EnumerateDirectories(
                     resolvedUsersDirectory))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var directoryName =
                Path.GetFileName(
                    directory.TrimEnd(
                        Path.DirectorySeparatorChar));

            if (string.IsNullOrWhiteSpace(directoryName))
                continue;

            var isSystemProfile =
                directoryName.Equals(
                    "Public",
                    StringComparison.OrdinalIgnoreCase)
                ||
                directoryName.Equals(
                    "Default",
                    StringComparison.OrdinalIgnoreCase)
                ||
                directoryName.Equals(
                    "Default User",
                    StringComparison.OrdinalIgnoreCase)
                ||
                directoryName.Equals(
                    "All Users",
                    StringComparison.OrdinalIgnoreCase);

            var sizeBytes =
                CalculateDirectorySize(
                    directory,
                    cancellationToken);

            profiles.Add(
                new UserProfileInformation
                {
                    UserName = directoryName,

                    ProfilePath = directory,

                    SizeBytes = sizeBytes,

                    IsCurrentUser =
                        directoryName.Equals(
                            currentUser,
                            StringComparison.OrdinalIgnoreCase),

                    IsSystemProfile =
                        isSystemProfile
                });
        }

        return Task.FromResult<
            IReadOnlyList<UserProfileInformation>>(
            profiles);
    }

    private static long CalculateDirectorySize(
        string directory,
        CancellationToken cancellationToken)
    {
        long total = 0;

        try
        {
            foreach (var file in
                     Directory.EnumerateFiles(
                         directory,
                         "*",
                         SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    total += new FileInfo(file).Length;
                }
                catch
                {
                    // Ignore files that cannot be accessed.
                }
            }
        }
        catch
        {
            // Ignore directories that cannot be accessed.
        }

        return total;
    }
}