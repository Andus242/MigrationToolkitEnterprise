using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MTE.App.ViewModels;
using MTE.Core.Interfaces;
using MTE.Engine.Engine;
using MTE.Engine.Logging;
using MTE.Engine.Verification;
using MTE.Engine.Services;
using MTE.Reporting;

namespace MTE.App;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    private static readonly string TraceFile = @"C:\MTE-Startup-Trace.txt";

    private static void Trace(string message)
    {
        try
        {
            File.AppendAllText(
                TraceFile,
                $"{DateTime.Now:HH:mm:ss.fff} - {message}{Environment.NewLine}");
        }
        catch
        {
            // Diagnostic logging must never prevent application startup.
        }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            Trace("1 - OnStartup entered");

            base.OnStartup(e);
            Trace("2 - base.OnStartup completed");

            var services = new ServiceCollection();
            Trace("3 - ServiceCollection created");

            ConfigureServices(services);
            Trace("4 - Services configured");

            _serviceProvider = services.BuildServiceProvider();
            Trace("5 - ServiceProvider built");

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            Trace("6 - MainWindow resolved");

            MainWindow = mainWindow;
            Trace("7 - MainWindow assigned");

            mainWindow.Show();
            Trace("8 - MainWindow.Show completed");
        }
        catch (Exception ex)
        {
            Trace("ERROR:");
            Trace(ex.ToString());

            Shutdown(1);
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IMigrationLogger, FileMigrationLogger>();
        services.AddSingleton<IMigrationEngine, MigrationEngine>();
        services.AddSingleton<IUserProfileDiscoveryService, UserProfileDiscoveryService>();
        services.AddSingleton<DriveDetectionService>();
        services.AddSingleton<IDataVerificationService, DataVerificationService>();
        services.AddSingleton<MigrationVerificationTestService>();
        services.AddSingleton<MigrationStorageService>();
	services.AddSingleton<KnownFolderService>();
        services.AddSingleton<FileMigrationService>();
        services.AddSingleton<FileCopyService>();
        services.AddSingleton<UserProfileMigrationService>();
        services.AddSingleton<ApplicationSettingsMigrationService>();
        services.AddSingleton<BrowserDataMigrationService>();
        services.AddSingleton<MigrationReportService>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Trace($"9 - OnExit called. ExitCode={e.ApplicationExitCode}");

        _serviceProvider?.Dispose();

        base.OnExit(e);
    }
}






