using System;

namespace Jellyfin.Plugin.TopCompare.Models;

/// <summary>
/// A movie present in both compared lists.
/// </summary>
public class SharedEntry
{
    /// <summary>
    /// Gets or sets the library item id.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the rank in the first list.
    /// </summary>
    public int RankA { get; set; }

    /// <summary>
    /// Gets or sets the rank in the second list.
    /// </summary>
    public int RankB { get; set; }

    /// <summary>
    /// Gets or sets the absolute rank difference.
    /// </summary>
    public int Difference { get; set; }
}
