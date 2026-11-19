using System.Collections.Generic;

namespace Jellyfin.Plugin.TopCompare.Models;

/// <summary>
/// Everything the plugin persists.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1002", Justification = "Persistence model")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227", Justification = "Persistence model")]
public class TopCompareData
{
    /// <summary>
    /// Gets or sets all topics.
    /// </summary>
    public List<Topic> Topics { get; set; } = [];

    /// <summary>
    /// Gets or sets all user rankings.
    /// </summary>
    public List<UserRanking> Rankings { get; set; } = [];
}
