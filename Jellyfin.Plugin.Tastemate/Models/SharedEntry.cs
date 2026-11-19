namespace Jellyfin.Plugin.Tastemate.Models;

/// <summary>
/// A movie present in both compared lists.
/// </summary>
public class SharedEntry
{
    /// <summary>
    /// Gets or sets the movie key, see <see cref="RankingEntry.Key"/>.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rank in the first list.
    /// </summary>
    public int RankA { get; set; }

    /// <summary>
    /// Gets or sets the rank in the second list.
    /// </summary>
    public int RankB { get; set; }

    /// <summary>
    /// Gets or sets the reason given in the first list.
    /// </summary>
    public string? ReasonA { get; set; }

    /// <summary>
    /// Gets or sets the reason given in the second list.
    /// </summary>
    public string? ReasonB { get; set; }

    /// <summary>
    /// Gets or sets the absolute rank difference.
    /// </summary>
    public int Difference { get; set; }
}
