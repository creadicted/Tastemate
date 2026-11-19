using System.Collections.Generic;

namespace Jellyfin.Plugin.Tastemate.Models;

/// <summary>
/// Everything the plugin persists.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1002", Justification = "Persistence model")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227", Justification = "Persistence model")]
public class TastemateData
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
