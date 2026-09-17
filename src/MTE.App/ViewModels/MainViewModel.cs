using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using MTE.Core.Interfaces;
using MTE.Core.Models;
using MTE.Engine.Services;
using MTE.Reporting;

namespace MTE.App.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly DriveDetectionService _driveDetectionService;

    private readonly IUserProfileDiscoveryService _userProfileDiscoveryService;
    private readonly IMigrationEngine _migrationEngine;
    private readonly MigrationVerificationTestService _verificationTestService;
    private readonly MigrationReportService _migrationReportService;

    private string _statusMessage = "Ready";
    private string _currentOperation = "Waiting for migration";
    private double _progressPercentage;
    private long _progressGeneration;
    private MigrationDestination? _selectedDestination;
    private CancellationTokenSource? _cancellationTokenSource;

    private bool _verifyBeforeMigration = true;
    private bool _operationRunning;
    public MainViewModel(
        DriveDetectionService driveDetectionService,
        IUserProfileDiscoveryService userProfileDiscoveryService,
        IMigrationEngine migrationEngine,
        MigrationVerificationTestService verificationTestService,
        MigrationReportService migrationReportService)
    {
        _driveDetectionService = driveDetectionService;

        _userProfileDiscoveryService = userProfileDiscoveryService;
        _migrationEngine = migrationEngine;
        _verificationTestService = verificationTestService;
        _migrationReportService = migrationReportService;

        RefreshDrivesCommand =
            new RelayCommand(
                RefreshDrives);

        
        DiscoverProfilesCommand =
            new RelayCommand(
                () => _ = DiscoverProfilesAsync(),
                () => !_operationRunning);

        SelectAllProfilesCommand =
            new RelayCommand(
                () =>
                {
                    StatusMessage = "SELECT ALL button clicked";
                    SelectAllProfiles();
                });

        ClearAllProfilesCommand =
            new RelayCommand(
                () =>
                {
                    StatusMessage = "CLEAR ALL button clicked";
                    ClearAllProfiles();
                });

        RunVerificationCommand =
            new RelayCommand(
                RunVerification,
                CanRunVerification);

        StartMigrationCommand =
            new RelayCommand(
                StartMigration,
                CanStartMigration);

        CancelMigrationCommand =
            new RelayCommand(
                CancelMigration,
                CanCancelOperation);

        RefreshDrives();

    }

    public ObservableCollection<MigrationDestination> AvailableDrives { get; }
        = new();

    
    public IReadOnlyList<string> CopyModes { get; } =
        new[]
        {
            "Whole Profile",
            "Selected Folders"
        };
    public ObservableCollection<SelectableUserProfile> AvailableProfiles { get; }
        = new();


    public ObservableCollection<SelectableUserProfile> SelectedProfiles { get; }
        = new();

    public int SelectedProfileCount =>
        SelectedProfiles.Count;

    public string ProfileSelectionSummary =>
        $"Profiles: {SelectedProfiles.Count} selected of {AvailableProfiles.Count} | Size: {SelectedProfiles.Sum(p => p.SizeBytes):N0} | SelectedSize: {SelectedProfiles.Sum(p => p.SelectedFoldersSizeBytes):N0} | Estimate: {EstimatedMigrationSizeDisplay}";

    public long EstimatedMigrationSizeBytes =>
        SelectedProfiles.Sum(profile => profile.SelectedFoldersSizeBytes);

    public string EstimatedMigrationSizeDisplay =>
        FormatSize(EstimatedMigrationSizeBytes);

public MigrationDestination? SelectedDestination
    {
        get => _selectedDestination;

        set
        {
            if (_selectedDestination == value)
                return;

            _selectedDestination = value;

            OnPropertyChanged();
            RaiseCommandStates();
        }
    }

    public bool VerifyBeforeMigration
    {
        get => _verifyBeforeMigration;

        set
        {
            if (_verifyBeforeMigration == value)
                return;

            _verifyBeforeMigration = value;

            OnPropertyChanged();
        }
    }

    public string DestinationStatus =>
        AvailableDrives.Count == 0
            ? "No suitable migration drives detected."
            : $"{AvailableDrives.Count} drive(s) available.";

    public string StatusMessage
    {
        get => _statusMessage;

        private set
        {
            if (_statusMessage == value)
                return;

            _statusMessage = value;

            OnPropertyChanged();
        }
    }

    public string CurrentOperation
    {
        get => _currentOperation;

        private set
        {
            if (_currentOperation == value)
                return;

            _currentOperation = value;

            OnPropertyChanged();
        }
    }

    public double ProgressPercentage
    {
        get => _progressPercentage;

        private set
        {
            var safeValue = Math.Clamp(value, 0d, 100d);

            if (Math.Abs(_progressPercentage - safeValue) < 0.01)
                return;

            void Update()
            {
                if (Math.Abs(_progressPercentage - safeValue) < 0.01)
                    return;

                _progressPercentage = safeValue;
                OnPropertyChanged(nameof(ProgressPercentage));
            }

            if (Application.Current?.Dispatcher.CheckAccess() == true)
            {
                Update();
            }
            else
            {
                Application.Current?.Dispatcher.Invoke(Update);
            }
        }
    }

    private long BeginProgressOperation()
    {
        return Interlocked.Increment(ref _progressGeneration);
    }

    private bool IsCurrentProgressOperation(long generation)
    {
        return Volatile.Read(ref _progressGeneration) == generation;
    }

    private void ResetProgress()
    {
        Interlocked.Increment(ref _progressGeneration);

        if (Math.Abs(_progressPercentage) < 0.01)
            return;

        _progressPercentage = 0d;
        OnPropertyChanged(nameof(ProgressPercentage));
    }
    public ICommand DiscoverProfilesCommand { get; }

    public ICommand RefreshDrivesCommand { get; }

    public ICommand SelectAllProfilesCommand { get; }

    public ICommand ClearAllProfilesCommand { get; }
    public ICommand RunVerificationCommand { get; }

    public ICommand StartMigrationCommand { get; }

    public ICommand CancelMigrationCommand { get; }

    public Task InitializeAsync()
    {
        return DiscoverProfilesAsync();
    }

    private async Task DiscoverProfilesAsync()
    {
        if (_operationRunning)
            return;

        try
        {
            _operationRunning = true;

            StatusMessage = "Discovering user profiles...";
            CurrentOperation = "Scanning user profiles";

            AvailableProfiles.Clear();
            SelectedProfiles.Clear();

            var profiles =
                await _userProfileDiscoveryService.DiscoverAsync(
                    CancellationToken.None);

            foreach (var profile in profiles)
            {
                var selectableProfile =
                    new SelectableUserProfile(
                        profile,
                        isSelected:
                            !profile.IsSystemProfile);
                selectableProfile.SelectionChanged +=
                    OnProfileSelectionChanged;

                selectableProfile.PropertyChanged +=
                    OnProfilePropertyChanged;

                AvailableProfiles.Add(
                    selectableProfile);

                if (selectableProfile.IsSelected)
                {
                    SelectedProfiles.Add(
                        selectableProfile);
                }
            }
            StatusMessage =
                $"{AvailableProfiles.Count} user profile(s) found.";
            CurrentOperation =
                "Calculating profile sizes...";

            await CalculateProfileSizesAsync();

            var availableDebug =
                AvailableProfiles
                    .FirstOrDefault(p => p.UserName.Equals(
                        Environment.UserName,
                        StringComparison.OrdinalIgnoreCase));

            var selectedDebug =
                SelectedProfiles
                    .FirstOrDefault(p => p.UserName.Equals(
                        Environment.UserName,
                        StringComparison.OrdinalIgnoreCase));

            var refDebug =
                $"Available={(availableDebug is null ? "NULL" : availableDebug.SizeBytes.ToString("N0"))}, " +
                $"Selected={(selectedDebug is null ? "NULL" : selectedDebug.SizeBytes.ToString("N0"))}, " +
                $"SameRef={ReferenceEquals(availableDebug, selectedDebug)}, " +
                $"SelectedCount={SelectedProfiles.Count}, " +
                $"OperationRunning={_operationRunning}, " +
                $"CanStartMigration={CanStartMigration()}, " +
                $"CanRunVerification={CanRunVerification()}";

            File.WriteAllText(
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "MTE-REF-DEBUG.txt"),
                refDebug);

            StatusMessage = $"REF DEBUG: {refDebug}";

            CurrentOperation =
                "Select the profiles to migrate";

        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Profile discovery cancelled.";
            CurrentOperation = "Waiting for migration";
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Profile discovery failed: {ex.Message}";

            CurrentOperation =
                "Unable to discover profiles";
        }
        finally
        {
            _operationRunning = false;

            var finalCommandDebug =
                $"SelectedProfiles={SelectedProfiles.Count}, " +
                $"OperationRunning={_operationRunning}, " +
                $"CanStartMigration={CanStartMigration()}, " +
                $"CanRunVerification={CanRunVerification()}";

            File.WriteAllText(
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "MTE-FINAL-COMMAND-DEBUG.txt"),
                finalCommandDebug);

            RaiseCommandStates();
        }
    }
    private async Task CalculateProfileSizesAsync()
    {
        var profilesToCalculate =
            AvailableProfiles
                .Where(profile => !profile.IsSystemProfile)
                .ToList();

        if (profilesToCalculate.Count == 0)
            return;

        var completed = 0;

        foreach (var profile in profilesToCalculate)
        {
            CurrentOperation =
                $"Calculating profile sizes... {completed + 1}/{profilesToCalculate.Count}";

            // Calculate the complete profile independently.
            // A folder-size calculation must never overwrite this value.
            try
            {
                var size =
                    await Task.Run(
                        () => CalculateDirectorySize(
                            profile.ProfilePath));

                StatusMessage =
                    $"SIZE DEBUG: {profile.UserName} = {size:N0} bytes ({FormatSize(size)})";

                profile.UpdateSize(size);
            }
            catch
            {
                profile.UpdateSize(0);
            }

            // Calculate the standard folder sizes independently.
            try
            {
                var folderSizes =
                    await Task.Run(
                        () => CalculateProfileFolderSizes(
                            profile.ProfilePath));

                profile.UpdateFolderSizes(
                    folderSizes.DesktopSizeBytes,
                    folderSizes.DocumentsSizeBytes,
                    folderSizes.DownloadsSizeBytes,
                    folderSizes.PicturesSizeBytes,
                    folderSizes.MusicSizeBytes,
                    folderSizes.VideosSizeBytes,
                    folderSizes.FavoritesSizeBytes);
            }
            catch
            {
                // Keep the complete profile size even if
                // one of the optional folder calculations fails.
            }
            completed++;
        }

        OnPropertyChanged(nameof(EstimatedMigrationSizeBytes));
        OnPropertyChanged(nameof(EstimatedMigrationSizeDisplay));
        StatusMessage =
            $"ESTIMATE DEBUG: Selected={SelectedProfiles.Count}, " +
            $"Total={EstimatedMigrationSizeBytes:N0} bytes, " +
            $"Display={EstimatedMigrationSizeDisplay}";
    }

    private static ProfileFolderSizes CalculateProfileFolderSizes(
        string profilePath)
    {
        var desktop = CalculateDirectorySize(
            Path.Combine(profilePath, "Desktop"));

        var documents = CalculateDirectorySize(
            Path.Combine(profilePath, "Documents"));

        var downloads = CalculateDirectorySize(
            Path.Combine(profilePath, "Downloads"));

        var pictures = CalculateDirectorySize(
            Path.Combine(profilePath, "Pictures"));

        var music = CalculateDirectorySize(
            Path.Combine(profilePath, "Music"));

        var videos = CalculateDirectorySize(
            Path.Combine(profilePath, "Videos"));

        var favorites = CalculateDirectorySize(
            Path.Combine(profilePath, "Favorites"));

        return new ProfileFolderSizes(
            desktop,
            documents,
            downloads,
            pictures,
            music,
            videos,
            favorites);
    }

    private sealed record ProfileFolderSizes(
        long DesktopSizeBytes,
        long DocumentsSizeBytes,
        long DownloadsSizeBytes,
        long PicturesSizeBytes,
        long MusicSizeBytes,
        long VideosSizeBytes,
        long FavoritesSizeBytes)
    {
        public long TotalSizeBytes =>
            CalculateDirectorySizeForTotal(
                DesktopSizeBytes,
                DocumentsSizeBytes,
                DownloadsSizeBytes,
                PicturesSizeBytes,
                MusicSizeBytes,
                VideosSizeBytes,
                FavoritesSizeBytes);
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";

        double size = bytes;
        string[] units = { "KB", "MB", "GB", "TB" };
        var unitIndex = -1;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:0.##} {units[unitIndex]}";
    }

    private static long CalculateDirectorySizeForTotal(
        params long[] sizes)
    {
        long total = 0;

        foreach (var size in sizes)
        {
            try
            {
                checked
                {
                    total += size;
                }
            }
            catch (OverflowException)
            {
                return long.MaxValue;
            }
        }

        return total;
    }

    private static long CalculateDirectorySize(
        string directory)
    {
        if (!Directory.Exists(directory))
            return 0;

        long total = 0;

        var pending = new Stack<string>();
        pending.Push(directory);

        while (pending.Count > 0)
        {
            var currentDirectory = pending.Pop();

            try
            {
                foreach (var file in
                         Directory.EnumerateFiles(
                             currentDirectory,
                             "*",
                             SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        var length =
                            new FileInfo(file).Length;

                        checked
                        {
                            total += length;
                        }
                    }
                    catch (OverflowException)
                    {
                        return long.MaxValue;
                    }
                    catch
                    {
                        // Ignore inaccessible files.
                    }
                }

                foreach (var subDirectory in
                         Directory.EnumerateDirectories(
                             currentDirectory,
                             "*",
                             SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        var attributes =
                            File.GetAttributes(subDirectory);

                        if ((attributes & FileAttributes.ReparsePoint) != 0)
                            continue;

                        pending.Push(subDirectory);
                    }
                    catch
                    {
                        // Ignore inaccessible directories.
                    }
                }
            }
            catch
            {
                // Ignore inaccessible directories.
            }
        }

        return total;
    }

    public void SelectAllProfiles()
    {
        if (_operationRunning)
            return;

        // Start with a clean selection collection.
        SelectedProfiles.Clear();

        // Select every profile displayed in the profile list.
        foreach (var profile in AvailableProfiles)
        {
            profile.IsSelected = true;

            if (!SelectedProfiles.Contains(profile))
                SelectedProfiles.Add(profile);
        }

        OnPropertyChanged(nameof(SelectedProfileCount));
        OnPropertyChanged(nameof(ProfileSelectionSummary));
        OnPropertyChanged(nameof(EstimatedMigrationSizeBytes));
        OnPropertyChanged(nameof(EstimatedMigrationSizeDisplay));

        StatusMessage =
            $"{SelectedProfiles.Count} profile(s) selected.";

        RaiseCommandStates();
    }

    public void ClearAllProfiles()
    {
        if (_operationRunning)
            return;

        // Clear the visual selection on every profile.
        foreach (var profile in AvailableProfiles)
        {
            profile.IsSelected = false;
        }

        // Explicitly clear the migration selection collection.
        SelectedProfiles.Clear();

        OnPropertyChanged(nameof(SelectedProfileCount));
        OnPropertyChanged(nameof(ProfileSelectionSummary));
        OnPropertyChanged(nameof(EstimatedMigrationSizeBytes));
        OnPropertyChanged(nameof(EstimatedMigrationSizeDisplay));

        StatusMessage =
            "All profiles cleared.";

        RaiseCommandStates();
    }

    private void OnProfilePropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectableUserProfile.SelectedFoldersSizeBytes)
            || e.PropertyName == nameof(SelectableUserProfile.SizeBytes))
        {
            OnPropertyChanged(nameof(EstimatedMigrationSizeBytes));
            OnPropertyChanged(nameof(EstimatedMigrationSizeDisplay));
        }
    }
    private void OnProfileSelectionChanged(
        object? sender,
        EventArgs e)
    {
        if (sender is not SelectableUserProfile profile)
            return;

        if (profile.IsSelected)
        {
            if (!SelectedProfiles.Contains(profile))
            {
                SelectedProfiles.Add(profile);
            }
        }
        else
        {
            SelectedProfiles.Remove(profile);
        }

        OnPropertyChanged(nameof(SelectedProfileCount));
        OnPropertyChanged(nameof(ProfileSelectionSummary));
        OnPropertyChanged(nameof(EstimatedMigrationSizeBytes));
        OnPropertyChanged(nameof(EstimatedMigrationSizeDisplay));

        StatusMessage =
            $"{SelectedProfiles.Count} profile(s) selected.";

        RaiseCommandStates();
    }

    private void RefreshDrives()
    {
        var previousDriveLetter =
            SelectedDestination?.DriveLetter;

        try
        {
            AvailableDrives.Clear();

            var drives =
                _driveDetectionService.GetAvailableDrives();

            foreach (var drive in drives)
            {
                AvailableDrives.Add(drive);
            }

            OnPropertyChanged(nameof(DestinationStatus));

            if (AvailableDrives.Count == 0)
            {
                SelectedDestination = null;

                StatusMessage =
                    "No suitable migration drives detected.";

                CurrentOperation =
                    "Waiting for migration destination";

                RaiseCommandStates();

                return;
            }

            StatusMessage =
                "Drives refreshed";

            CurrentOperation =
                $"{AvailableDrives.Count} drive(s) detected.";

            SelectedDestination =
                AvailableDrives.FirstOrDefault(
                    d =>
                        !string.IsNullOrWhiteSpace(previousDriveLetter) &&
                        string.Equals(
                            d.DriveLetter,
                            previousDriveLetter,
                            StringComparison.OrdinalIgnoreCase))
                ?? AvailableDrives.FirstOrDefault(
                    d => d.IsRecommended)
                ?? AvailableDrives.FirstOrDefault();

            var destinationDebug =
                SelectedDestination is null
                    ? "SelectedDestination=NULL"
                    : $"SelectedDestination={SelectedDestination.DriveLetter}, " +
                      $"IsSystemDrive={SelectedDestination.IsSystemDrive}, " +
                      $"IsExternal={SelectedDestination.IsExternal}, " +
                      $"IsRemovable={SelectedDestination.IsRemovable}, " +
                      $"AvailableDrives={AvailableDrives.Count}, " +
                      $"SelectedProfiles={SelectedProfiles.Count}, " +
                      $"OperationRunning={_operationRunning}, " +
                      $"CanStartMigration={CanStartMigration()}, " +
                      $"CanRunVerification={CanRunVerification()}";

            File.WriteAllText(
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "MTE-DESTINATION-DEBUG.txt"),
                destinationDebug);

            RaiseCommandStates();
        }
        catch (Exception ex)
        {
            StatusMessage =
                "Unable to detect drives.";

            CurrentOperation =
                ex.Message;

            RaiseCommandStates();
        }
    }

    private bool CanRunVerification()
    {
        var destination = SelectedDestination;

        return !_operationRunning
            && destination is not null
            && !destination.IsSystemDrive;
    }

    private bool CanStartMigration()
    {
        var destination = SelectedDestination;

        return !_operationRunning
            && SelectedProfiles.Count > 0
            && destination is not null
            && !destination.IsSystemDrive;
    }

    private bool CanCancelOperation()
    {
        var cancellationTokenSource =
            _cancellationTokenSource;

        return _operationRunning
            && cancellationTokenSource is not null
            && !cancellationTokenSource.IsCancellationRequested;
    }

    private async void RunVerification()
    {
        await RunVerificationInternalAsync(false);
    }

    private async Task<bool> RunVerificationInternalAsync(
        bool asPartOfMigration)
    {
        var destination =
            SelectedDestination;

        if (destination is null)
        {
            StatusMessage =
                "Please select a migration destination.";

            CurrentOperation =
                "Verification cannot start";

            return false;
        }

        if (destination.IsSystemDrive)
        {
            StatusMessage =
                "The system drive cannot be used as the verification destination.";

            CurrentOperation =
                "Please select another drive.";

            return false;
        }

        try
        {
            SetOperationRunning(true);

            _cancellationTokenSource?.Dispose();

            _cancellationTokenSource =
                new CancellationTokenSource();

            RaiseCommandStates();

            ResetProgress();

            StatusMessage =
                asPartOfMigration
                    ? "Pre-migration verification started"
                    : "Verification started";

            CurrentOperation =
                $"Testing migration destination {destination.DisplayName}";

            var progressGeneration =
                BeginProgressOperation();

            var progress = new Progress<double>(
                value =>
                {
                    if (!IsCurrentProgressOperation(progressGeneration))
                        return;

                    ProgressPercentage = value;
                });
            var result =
                await _verificationTestService.RunTestAsync(
                    destination.DriveLetter + "\\",
                    progress,
                    _cancellationTokenSource.Token);

            if (result.Verified)
            {
                ProgressPercentage = 100;

                StatusMessage =
                    asPartOfMigration
                        ? "Pre-migration verification passed"
                        : "Verification passed";

                CurrentOperation =
                    $"Test file successfully copied and verified on {destination.DisplayName}";

                return true;
            }

            StatusMessage =
                "Verification failed";

            CurrentOperation =
                $"Verification result: {result.Status}";

            return false;
        }
        catch (OperationCanceledException)
        {
            StatusMessage =
                "Verification cancelled";

            CurrentOperation =
                "Waiting for migration";

            ResetProgress();

            return false;
        }
        catch (Exception ex)
        {
            StatusMessage =
                "Verification failed";

            CurrentOperation =
                ex.Message;

            ResetProgress();

            return false;
        }
        finally
        {
            _cancellationTokenSource?.Dispose();

            _cancellationTokenSource = null;

            SetOperationRunning(false);

            RaiseCommandStates();
        }
    }

    private async void StartMigration()
    {
        var destination =
            SelectedDestination;

        if (destination is null)
        {
            StatusMessage =
                "Please select a migration destination.";

            CurrentOperation =
                "Migration cannot start";

            RaiseCommandStates();

            return;
        }

        if (destination.IsSystemDrive)
        {
            StatusMessage =
                "The system drive cannot be used as the migration destination.";

            CurrentOperation =
                "Please select another drive.";

            RaiseCommandStates();

            return;
        }

        if (_operationRunning)
        {
            return;
        }

        try
        {
            SetOperationRunning(true);

            _cancellationTokenSource?.Dispose();

            _cancellationTokenSource =
                new CancellationTokenSource();

            RaiseCommandStates();

            ResetProgress();

            StatusMessage =
                "Preparing migration";

            CurrentOperation =
                $"Preparing migration to {destination.DisplayName}";

            if (VerifyBeforeMigration)
            {
                var verificationPassed =
                    await RunPreMigrationVerificationAsync(
                        _cancellationTokenSource.Token);

                if (!verificationPassed)
                {
                    if (!_cancellationTokenSource.IsCancellationRequested)
                    {
                        StatusMessage =
                            "Migration stopped";

                        CurrentOperation =
                            "Pre-migration verification did not pass. Migration was not started.";
                    }

                    return;
                }

                ResetProgress();

                StatusMessage =
                    "Verification passed";

                CurrentOperation =
                    "Destination verified. Starting migration...";
            }

            var reportStartedAt =
                DateTime.Now;

            var migrationProgressGeneration =
                BeginProgressOperation();

            var progress =
                new Progress<MigrationProgressInfo>(
                    info =>
                    {
                        try
                        {
                            File.AppendAllText(
                                @"C:\MTE-Progress-Debug.txt",
                                $"{DateTime.Now:HH:mm:ss.fff} - CALLBACK: Section={info.Section}, Percentage={info.Percentage}, Complete={info.IsComplete}, Message={info.Message}{Environment.NewLine}");
                        }
                        catch
                        {
                        }

                        if (!IsCurrentProgressOperation(
                                migrationProgressGeneration))
                        {
                            return;
                        }

                        ProgressPercentage =
                            Math.Clamp(
                                info.Percentage,
                                0d,
                                100d);

                        CurrentOperation =
                            info.Message;

                        if (info.IsComplete)
                        {
                            StatusMessage =
                                "Migration completed successfully.";
                        }
                        else
                        {
                            StatusMessage =
                                $"Migration in progress - {info.Section}";
                        }
                    });

            var cancellationTokenSource =
                _cancellationTokenSource;

            if (cancellationTokenSource is null)
            {
                StatusMessage =
                    "Migration failed";

                CurrentOperation =
                    "Migration cancellation source was not available.";

                return;
            }
            var selectedProfiles =
                SelectedProfiles
                    .Select(profile =>
                        new MigrationProfileSelection
                        {
                            Profile = profile.Profile,
                            SelectedFolders = profile.SelectedFolders
                        })
                    .ToList();

            var migrationResult =
                await _migrationEngine.ExecuteAsync(
                    destination.DriveLetter,
                    selectedProfiles,
                    progress,
                    cancellationTokenSource.Token);
            var migrationRoot =
                Path.Combine(
                    destination.DriveLetter.TrimEnd('\\'),
                    "MTE Migration");

            var report =
                new MigrationReport
                {
                    Destination = destination.DriveLetter,
                    StartedAt = reportStartedAt,
                    CompletedAt = DateTime.Now,
                    Status = "Completed",
                    MigrationMode =
                        selectedProfiles.Any(
                            profile =>
                                profile.SelectedFolders is not null)
                            ? "Selected Folders"
                            : "Whole Profile",
                    TotalFiles = migrationResult.TotalFiles,
                    TotalBytes = migrationResult.TotalBytes,
                    CopiedFiles = migrationResult.CopiedFiles,
                    CopiedBytes = migrationResult.CopiedBytes,
                    VerifiedFiles = migrationResult.VerifiedFiles,
                    FailedFiles = migrationResult.FailedFiles,
                    SelectedProfiles =
                        selectedProfiles
                            .Select(profile =>
                                profile.Profile.UserName)
                            .ToList(),
                    SelectedFolders =
                        selectedProfiles
                            .Where(profile =>
                                profile.SelectedFolders is not null)
                            .SelectMany(profile =>
                                profile.SelectedFolders!)
                            .Distinct(
                                StringComparer.OrdinalIgnoreCase)
                            .ToList(),
                    Sections =
                        migrationResult.Sections
                            .Select(section =>
                                new MigrationSectionResult
                                {
                                    Section = section.Section,
                                    Success = section.Success,
                                    Message = section.Message,
                                    Timestamp = section.Timestamp
                                })
                            .ToList()
                };

            _migrationReportService.SaveJson(
                report,
                migrationRoot);

            _migrationReportService.SaveHtml(
                report,
                migrationRoot);
        }
        catch (OperationCanceledException)
        {
            StatusMessage =
                "Migration cancelled";

            CurrentOperation =
                "Waiting for migration";

            ResetProgress();
        }
        catch (Exception ex)
        {
            StatusMessage =
                "Migration failed";

            CurrentOperation =
                ex.Message;
        }
        finally
        {
            _cancellationTokenSource?.Dispose();

            _cancellationTokenSource = null;

            SetOperationRunning(false);

            RaiseCommandStates();
        }
    }

    private async Task<bool> RunPreMigrationVerificationAsync(
        CancellationToken cancellationToken)
    {
        var destination =
            SelectedDestination;

        if (destination is null)
            return false;

        if (destination.IsSystemDrive)
        {
            StatusMessage =
                "Verification failed";

            CurrentOperation =
                "The system drive cannot be used as the migration destination.";

            return false;
        }

        ResetProgress();

        StatusMessage =
            "Pre-migration verification";

        CurrentOperation =
            $"Verifying migration destination {destination.DisplayName}...";

        try
        {
            var preMigrationProgressGeneration =
                BeginProgressOperation();

            var progress = new Progress<double>(
                value =>
                {
                    if (!IsCurrentProgressOperation(
                            preMigrationProgressGeneration))
                    {
                        return;
                    }

                    ProgressPercentage = value;
                });

            var result =
                await _verificationTestService.RunTestAsync(
                    destination.DriveLetter + "\\",
                    progress,
                    cancellationToken);
            if (!result.Verified)
            {
                StatusMessage =
                    "Verification failed";

                CurrentOperation =
                    $"Migration blocked: {result.Status}";

                return false;
            }

            StatusMessage =
                "Verification passed";

            CurrentOperation =
                "Migration destination verified successfully.";

            return true;
        }
        catch (OperationCanceledException)
        {
            StatusMessage =
                "Verification cancelled";

            CurrentOperation =
                "Migration cancelled";

            throw;
        }
        catch (Exception ex)
        {
            StatusMessage =
                "Verification failed";

            CurrentOperation =
                $"Migration blocked: {ex.Message}";

            return false;
        }
    }

    private void CancelMigration()
    {
        if (!_operationRunning)
        {
            StatusMessage =
                "No operation is currently running.";

            CurrentOperation =
                "Waiting for migration";

            return;
        }

        var cancellationTokenSource =
            _cancellationTokenSource;

        if (cancellationTokenSource is null)
        {
            StatusMessage =
                "No cancellable operation is currently running.";

            CurrentOperation =
                "Waiting for migration";

            return;
        }

        if (cancellationTokenSource.IsCancellationRequested)
        {
            StatusMessage =
                "Cancellation already requested.";

            CurrentOperation =
                "Please wait...";

            return;
        }

        StatusMessage =
            "Cancelling operation...";

        CurrentOperation =
            "Please wait...";

        cancellationTokenSource.Cancel();

        RaiseCommandStates();
    }

    private void SetOperationRunning(bool running)
    {
        _operationRunning =
            running;

        RaiseCommandStates();
    }

    private void RaiseCommandStates()
    {
        if (RefreshDrivesCommand is RelayCommand refresh)
            refresh.RaiseCanExecuteChanged();

        if (DiscoverProfilesCommand is RelayCommand discover)
            discover.RaiseCanExecuteChanged();

        if (SelectAllProfilesCommand is RelayCommand selectAll)
            selectAll.RaiseCanExecuteChanged();

        if (ClearAllProfilesCommand is RelayCommand clearAll)
            clearAll.RaiseCanExecuteChanged();

        if (RunVerificationCommand is RelayCommand verification)
            verification.RaiseCanExecuteChanged();

        if (StartMigrationCommand is RelayCommand migration)
            migration.RaiseCanExecuteChanged();

        if (CancelMigrationCommand is RelayCommand cancel)
            cancel.RaiseCanExecuteChanged();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(
        Action execute,
        Func<bool>? canExecute = null)
    {
        _execute =
            execute ??
            throw new ArgumentNullException(nameof(execute));

        _canExecute =
            canExecute;
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke() ?? true;
    }

    public void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
            return;

        _execute();
    }

    public event EventHandler? CanExecuteChanged;

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(
            this,
            EventArgs.Empty);
    }
}







































































































