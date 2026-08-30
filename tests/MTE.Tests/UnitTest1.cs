using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MTE.Engine.Logging;
using MTE.Engine.Services;
using MTE.Engine.Verification;

namespace MTE.Tests;

public class UnitTest1
{
    private static readonly string[] StandardFolders =
    {
        "Desktop",
        "Documents",
        "Downloads",
        "Pictures",
        "Videos",
        "Music",
        "Favorites"
    };

    [Fact]
    public async Task WholeProfile_CopiesAllStandardFolders()
    {
        var root = CreateTestProfile();

        try
        {
            var source = Path.Combine(root, "SourceProfile");
            var destination = Path.Combine(root, "DestinationProfile");

            var service = CreateService();

            await service.CopyUserProfileAsync(
                source,
                destination,
                selectedFolders: null,
                progress: null,
                CancellationToken.None);

            foreach (var folder in StandardFolders)
            {
                var destinationFile =
                    Path.Combine(
                        destination,
                        folder,
                        "MTE-Test.txt");

                Assert.True(
                    File.Exists(destinationFile),
                    $"Expected folder '{folder}' to be copied.");
            }
        }
        finally
        {
            DeleteTestProfile(root);
        }
    }

    [Fact]
    public async Task SelectedFolders_CopiesOnlySelectedFolders()
    {
        var root = CreateTestProfile();

        try
        {
            var source = Path.Combine(root, "SourceProfile");
            var destination = Path.Combine(root, "DestinationProfile");

            var selectedFolders =
                new List<string>
                {
                    "Documents",
                    "Pictures"
                };

            var service = CreateService();

            await service.CopyUserProfileAsync(
                source,
                destination,
                selectedFolders,
                progress: null,
                CancellationToken.None);

            Assert.True(
                File.Exists(
                    Path.Combine(
                        destination,
                        "Documents",
                        "MTE-Test.txt")));

            Assert.True(
                File.Exists(
                    Path.Combine(
                        destination,
                        "Pictures",
                        "MTE-Test.txt")));

            foreach (var folder in StandardFolders.Except(selectedFolders))
            {
                Assert.False(
                    File.Exists(
                        Path.Combine(
                            destination,
                            folder,
                            "MTE-Test.txt")),
                    $"Folder '{folder}' should not have been copied.");
            }
        }
        finally
        {
            DeleteTestProfile(root);
        }
    }

    [Fact]
    public async Task EmptyFolderSelection_CopiesNothing()
    {
        var root = CreateTestProfile();

        try
        {
            var source = Path.Combine(root, "SourceProfile");
            var destination = Path.Combine(root, "DestinationProfile");

            var selectedFolders =
                Array.Empty<string>();

            var service = CreateService();

            await service.CopyUserProfileAsync(
                source,
                destination,
                selectedFolders,
                progress: null,
                CancellationToken.None);

            foreach (var folder in StandardFolders)
            {
                Assert.False(
                    File.Exists(
                        Path.Combine(
                            destination,
                            folder,
                            "MTE-Test.txt")),
                    $"Folder '{folder}' should not have been copied.");
            }
        }
        finally
        {
            DeleteTestProfile(root);
        }
    }

    private static UserProfileMigrationService CreateService()
    {
        var logger = new FileMigrationLogger();

        var verificationService =
            new DataVerificationService();

        var fileMigrationService =
            new FileMigrationService(
                verificationService,
                logger);

        return new UserProfileMigrationService(
            fileMigrationService,
            logger);
    }

    private static string CreateTestProfile()
    {
        var root =
            Path.Combine(
                Path.GetTempPath(),
                "MTE-ProfileSelection-UnitTest-" +
                Guid.NewGuid().ToString("N"));

        var source =
            Path.Combine(
                root,
                "SourceProfile");

        Directory.CreateDirectory(source);

        foreach (var folder in StandardFolders)
        {
            var folderPath =
                Path.Combine(
                    source,
                    folder);

            Directory.CreateDirectory(folderPath);

            File.WriteAllText(
                Path.Combine(
                    folderPath,
                    "MTE-Test.txt"),
                $"MTE TEST FILE - {folder}");
        }

        return root;
    }

    private static void DeleteTestProfile(string root)
    {
        try
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(
                    root,
                    recursive: true);
            }
        }
        catch
        {
            // Test cleanup should not hide the actual test result.
        }
    }
}
