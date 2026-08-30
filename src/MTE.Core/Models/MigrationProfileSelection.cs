namespace MTE.Core.Models;

public sealed class MigrationProfileSelection
{
    public UserProfileInformation Profile { get; init; } = new();

    /// <summary>
    /// Null means "Whole Profile".
    /// An empty list means "Selected Folders" with nothing selected.
    /// Otherwise only the specified standard folders are migrated.
    /// </summary>
    public IReadOnlyList<string>? SelectedFolders { get; init; }
}
