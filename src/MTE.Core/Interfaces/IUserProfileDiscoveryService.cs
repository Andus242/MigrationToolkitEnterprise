using MTE.Core.Models;

namespace MTE.Core.Interfaces;

public interface IUserProfileDiscoveryService
{
    Task<IReadOnlyList<UserProfileInformation>> DiscoverAsync(
        CancellationToken cancellationToken);
}