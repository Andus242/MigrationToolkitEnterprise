using MTE.Core.Models;

namespace MTE.Core.Interfaces;

public interface ISystemDiscoveryService
{
    Task<SystemInformation> DiscoverAsync(
        CancellationToken cancellationToken);
}