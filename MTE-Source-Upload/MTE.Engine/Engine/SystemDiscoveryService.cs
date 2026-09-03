using System.Runtime.InteropServices;
using MTE.Core.Interfaces;
using MTE.Core.Models;

namespace MTE.Engine.Engine;

public sealed class SystemDiscoveryService : ISystemDiscoveryService
{
    public Task<SystemInformation> DiscoverAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var information = new SystemInformation
        {
            ComputerName = Environment.MachineName,

            OperatingSystem = RuntimeInformation
                .OSDescription,

            WindowsVersion = Environment.OSVersion
                .VersionString,

            Architecture = RuntimeInformation
                .OSArchitecture
                .ToString(),

            CurrentUser = Environment.UserName,

            WindowsDirectory = Environment
                .GetFolderPath(
                    Environment.SpecialFolder.Windows),

            SystemDrive = Path.GetPathRoot(
                Environment.SystemDirectory)
                ?? string.Empty,

            ProcessorCount = Environment
                .ProcessorCount,

            TotalMemoryBytes = GC
                .GetGCMemoryInfo()
                .TotalAvailableMemoryBytes
        };

        return Task.FromResult(information);
    }
}