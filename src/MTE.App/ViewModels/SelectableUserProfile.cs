using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MTE.Core.Models;

namespace MTE.App.ViewModels;

public sealed class SelectableUserProfile : INotifyPropertyChanged
{
    private bool _isSelected;

    private string _copyMode = "Whole Profile";

    private bool _copyDesktop = true;
    private bool _copyDocuments = true;
    private bool _copyDownloads = true;
    private bool _copyPictures = true;
    private bool _copyMusic = true;
    private bool _copyVideos = true;
    private bool _copyFavorites = true;

    private long _desktopSizeBytes;
    private long _documentsSizeBytes;
    private long _downloadsSizeBytes;
    private long _picturesSizeBytes;
    private long _musicSizeBytes;
    private long _videosSizeBytes;
    private long _favoritesSizeBytes;

    public SelectableUserProfile(
        UserProfileInformation profile,
        bool isSelected = false)
    {
        Profile = profile;
        _isSelected = isSelected;
    }

    public UserProfileInformation Profile { get; private set; }

    public string UserName =>
        Profile.UserName;

    public string ProfilePath =>
        Profile.ProfilePath;

    public long SizeBytes =>
        Profile.SizeBytes;

    public long DesktopSizeBytes => _desktopSizeBytes;

    public long DocumentsSizeBytes => _documentsSizeBytes;

    public long DownloadsSizeBytes => _downloadsSizeBytes;

    public long PicturesSizeBytes => _picturesSizeBytes;

    public long MusicSizeBytes => _musicSizeBytes;

    public long VideosSizeBytes => _videosSizeBytes;

    public long FavoritesSizeBytes => _favoritesSizeBytes;

    public long SelectedFoldersSizeBytes
    {
        get
        {
            if (CopyWholeProfile)
                return SizeBytes;

            long total = 0;

            if (CopyDesktop)
                total += DesktopSizeBytes;

            if (CopyDocuments)
                total += DocumentsSizeBytes;

            if (CopyDownloads)
                total += DownloadsSizeBytes;

            if (CopyPictures)
                total += PicturesSizeBytes;

            if (CopyMusic)
                total += MusicSizeBytes;

            if (CopyVideos)
                total += VideosSizeBytes;

            if (CopyFavorites)
                total += FavoritesSizeBytes;

            return total;
        }
    }

    public string SelectedFoldersSizeDisplay =>
        FormatSize(SelectedFoldersSizeBytes);

    public string DesktopSizeDisplay =>
        FormatSize(DesktopSizeBytes);

    public string DocumentsSizeDisplay =>
        FormatSize(DocumentsSizeBytes);

    public string DownloadsSizeDisplay =>
        FormatSize(DownloadsSizeBytes);

    public string PicturesSizeDisplay =>
        FormatSize(PicturesSizeBytes);

    public string MusicSizeDisplay =>
        FormatSize(MusicSizeBytes);

    public string VideosSizeDisplay =>
        FormatSize(VideosSizeBytes);

    public string FavoritesSizeDisplay =>
        FormatSize(FavoritesSizeBytes);

    public string SizeDisplay
    {
        get
        {
            const double mb = 1024d * 1024d;
            const double gb = mb * 1024d;

            if (SizeBytes >= gb)
                return $"{SizeBytes / gb:N1} GB";

            if (SizeBytes >= mb)
                return $"{SizeBytes / mb:N1} MB";

            if (SizeBytes >= 1024)
                return $"{SizeBytes / 1024d:N1} KB";

            return $"{SizeBytes:N0} bytes";
        }
    }

    public bool IsCurrentUser =>
        Profile.IsCurrentUser;

    public bool IsSystemProfile =>
        Profile.IsSystemProfile;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;

            _isSelected = value;
            OnPropertyChanged();

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public string CopyMode
    {
        get => _copyMode;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            if (_copyMode == value)
                return;

            _copyMode = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(CopyWholeProfile));
            OnPropertyChanged(nameof(FolderSelectionEnabled));
            OnPropertyChanged(nameof(SelectedFoldersDisplay));
            OnPropertyChanged(nameof(SelectedFoldersSizeBytes));
            OnPropertyChanged(nameof(SelectedFoldersSizeDisplay));
        }
    }

    public bool CopyWholeProfile =>
        string.Equals(
            CopyMode,
            "Whole Profile",
            StringComparison.OrdinalIgnoreCase);

    public bool FolderSelectionEnabled =>
        !CopyWholeProfile;

    public bool CopyDesktop
    {
        get => _copyDesktop;
        set
        {
            if (_copyDesktop == value)
                return;

            _copyDesktop = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedFoldersDisplay));
            OnPropertyChanged(nameof(SelectedFoldersSizeBytes));
            OnPropertyChanged(nameof(SelectedFoldersSizeDisplay));
        }
    }

    public bool CopyDocuments
    {
        get => _copyDocuments;
        set
        {
            if (_copyDocuments == value)
                return;

            _copyDocuments = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedFoldersDisplay));
            OnPropertyChanged(nameof(SelectedFoldersSizeBytes));
            OnPropertyChanged(nameof(SelectedFoldersSizeDisplay));
        }
    }

    public bool CopyDownloads
    {
        get => _copyDownloads;
        set
        {
            if (_copyDownloads == value)
                return;

            _copyDownloads = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedFoldersDisplay));
            OnPropertyChanged(nameof(SelectedFoldersSizeBytes));
            OnPropertyChanged(nameof(SelectedFoldersSizeDisplay));
        }
    }

    public bool CopyPictures
    {
        get => _copyPictures;
        set
        {
            if (_copyPictures == value)
                return;

            _copyPictures = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedFoldersDisplay));
            OnPropertyChanged(nameof(SelectedFoldersSizeBytes));
            OnPropertyChanged(nameof(SelectedFoldersSizeDisplay));
        }
    }

    public bool CopyMusic
    {
        get => _copyMusic;
        set
        {
            if (_copyMusic == value)
                return;

            _copyMusic = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedFoldersDisplay));
            OnPropertyChanged(nameof(SelectedFoldersSizeBytes));
            OnPropertyChanged(nameof(SelectedFoldersSizeDisplay));
        }
    }

    public bool CopyVideos
    {
        get => _copyVideos;
        set
        {
            if (_copyVideos == value)
                return;

            _copyVideos = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedFoldersDisplay));
            OnPropertyChanged(nameof(SelectedFoldersSizeBytes));
            OnPropertyChanged(nameof(SelectedFoldersSizeDisplay));
        }
    }

    public bool CopyFavorites
    {
        get => _copyFavorites;
        set
        {
            if (_copyFavorites == value)
                return;

            _copyFavorites = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedFoldersDisplay));
            OnPropertyChanged(nameof(SelectedFoldersSizeBytes));
            OnPropertyChanged(nameof(SelectedFoldersSizeDisplay));
        }
    }

    public IReadOnlyList<string>? SelectedFolders
    {
        get
        {
            if (CopyWholeProfile)
                return null;

            var folders = new List<string>();

            if (CopyDesktop)
                folders.Add("Desktop");

            if (CopyDocuments)
                folders.Add("Documents");

            if (CopyDownloads)
                folders.Add("Downloads");

            if (CopyPictures)
                folders.Add("Pictures");

            if (CopyMusic)
                folders.Add("Music");

            if (CopyVideos)
                folders.Add("Videos");

            if (CopyFavorites)
                folders.Add("Favorites");

            return folders;
        }
    }

    public string SelectedFoldersDisplay
    {
        get
        {
            if (CopyWholeProfile)
                return "Entire profile";

            var folders = SelectedFolders;

            if (folders is null)
                return "Entire profile";

            if (folders.Count == 0)
                return "No folders selected";

            return string.Join(", ", folders);
        }
    }

    public event EventHandler? SelectionChanged;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void UpdateFolderSizes(
        long desktopSizeBytes,
        long documentsSizeBytes,
        long downloadsSizeBytes,
        long picturesSizeBytes,
        long musicSizeBytes,
        long videosSizeBytes,
        long favoritesSizeBytes)
    {
        _desktopSizeBytes = desktopSizeBytes;
        _documentsSizeBytes = documentsSizeBytes;
        _downloadsSizeBytes = downloadsSizeBytes;
        _picturesSizeBytes = picturesSizeBytes;
        _musicSizeBytes = musicSizeBytes;
        _videosSizeBytes = videosSizeBytes;
        _favoritesSizeBytes = favoritesSizeBytes;

        OnPropertyChanged(nameof(DesktopSizeBytes));
        OnPropertyChanged(nameof(DocumentsSizeBytes));
        OnPropertyChanged(nameof(DownloadsSizeBytes));
        OnPropertyChanged(nameof(PicturesSizeBytes));
        OnPropertyChanged(nameof(MusicSizeBytes));
        OnPropertyChanged(nameof(VideosSizeBytes));
        OnPropertyChanged(nameof(FavoritesSizeBytes));

        OnPropertyChanged(nameof(DesktopSizeDisplay));
        OnPropertyChanged(nameof(DocumentsSizeDisplay));
        OnPropertyChanged(nameof(DownloadsSizeDisplay));
        OnPropertyChanged(nameof(PicturesSizeDisplay));
        OnPropertyChanged(nameof(MusicSizeDisplay));
        OnPropertyChanged(nameof(VideosSizeDisplay));
        OnPropertyChanged(nameof(FavoritesSizeDisplay));

        OnPropertyChanged(nameof(SelectedFoldersSizeBytes));
        OnPropertyChanged(nameof(SelectedFoldersSizeDisplay));
    }

    public void UpdateSize(long sizeBytes)
    {
        Profile = new UserProfileInformation
        {
            UserName = Profile.UserName,
            ProfilePath = Profile.ProfilePath,
            SizeBytes = sizeBytes,
            IsCurrentUser = Profile.IsCurrentUser,
            IsSystemProfile = Profile.IsSystemProfile,
            DiscoveredAt = Profile.DiscoveredAt
        };

        OnPropertyChanged(nameof(SizeBytes));
        OnPropertyChanged(nameof(SizeDisplay));

        // SelectedFoldersSizeBytes depends on SizeBytes
        // when the profile is in Whole Profile mode.
        OnPropertyChanged(nameof(SelectedFoldersSizeBytes));
        OnPropertyChanged(nameof(SelectedFoldersSizeDisplay));
    }

    private static string FormatSize(long bytes)
    {
        const double kb = 1024d;
        const double mb = kb * 1024d;
        const double gb = mb * 1024d;
        const double tb = gb * 1024d;

        if (bytes >= tb)
            return $"{bytes / tb:N1} TB";

        if (bytes >= gb)
            return $"{bytes / gb:N1} GB";

        if (bytes >= mb)
            return $"{bytes / mb:N1} MB";

        if (bytes >= kb)
            return $"{bytes / kb:N1} KB";

        return $"{bytes:N0} B";
    }

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}





