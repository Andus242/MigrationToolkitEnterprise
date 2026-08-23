using System;
using System.Collections.Generic;
using System.IO;

namespace MTE.Engine.Services;

public sealed class KnownFolderService
{
    public IReadOnlyList<KnownFolderInfo> GetUserFolders()
    {
        var folders = new List<KnownFolderInfo>();

        AddFolder(
            folders,
            "Desktop",
            Environment.GetFolderPath(
                Environment.SpecialFolder.DesktopDirectory));

        AddFolder(
            folders,
            "Documents",
            Environment.GetFolderPath(
                Environment.SpecialFolder.MyDocuments));

        AddFolder(
            folders,
            "Pictures",
            Environment.GetFolderPath(
                Environment.SpecialFolder.MyPictures));

        AddFolder(
            folders,
            "Music",
            Environment.GetFolderPath(
                Environment.SpecialFolder.MyMusic));

        AddFolder(
            folders,
            "Videos",
            Environment.GetFolderPath(
                Environment.SpecialFolder.MyVideos));

        AddFolder(
            folders,
            "Downloads",
            GetDownloadsPath());

        AddFolder(
            folders,
            "Favorites",
            Environment.GetFolderPath(
                Environment.SpecialFolder.Favorites));

        return folders;
    }

    private static void AddFolder(
        List<KnownFolderInfo> folders,
        string name,
        string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            folders.Add(
                new KnownFolderInfo
                {
                    Name = name,
                    Path = string.Empty,
                    Exists = false,
                    IsOneDriveRedirected = false
                });

            return;
        }

        folders.Add(
            new KnownFolderInfo
            {
                Name = name,
                Path = path,
                Exists = Directory.Exists(path),
                IsOneDriveRedirected =
                    IsOneDrivePath(path)
            });
    }

    private static string GetDownloadsPath()
    {
        var downloadsPath =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.UserProfile),
                "Downloads");

        return downloadsPath;
    }

    private static bool IsOneDrivePath(string path)
    {
        var normalized =
            path.Replace(
                Path.AltDirectorySeparatorChar,
                Path.DirectorySeparatorChar);

        var parts =
            normalized.Split(
                Path.DirectorySeparatorChar,
                StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            if (part.Equals(
                    "OneDrive",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (part.StartsWith(
                    "OneDrive - ",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

public sealed class KnownFolderInfo
{
    public string Name { get; init; } = string.Empty;

    public string Path { get; init; } = string.Empty;

    public bool Exists { get; init; }

    public bool IsOneDriveRedirected { get; init; }
}
