using System;

namespace Jellyfin.Plugin.TopCompare.Models;

/// <summary>
/// One ranked movie.
/// </summary>
public class RankingEntry
{
    /// <summary>
    /// Gets or sets the 1-based rank.
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// Gets or sets the library item id of the movie.
    /// </summary>
    public Guid ItemId { get; set; }
}
