namespace MTE.Core.Models;

public sealed class UserProfileInformation
{
    public string UserName { get; init; } = string.Empty;

    public string ProfilePath { get; init; } = string.Empty;

    public long SizeBytes { get; set; }

    public bool IsCurrentUser { get; init; }

    public bool IsSystemProfile { get; init; }

    public DateTime DiscoveredAt { get; init; } = DateTime.Now;
}
