using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.Tastemate.Models;

/// <summary>
/// A user's ranking for a topic.
/// </summary>
public class UserRanking
{
    /// <summary>
    /// Gets or sets the topic id.
    /// </summary>
    public Guid TopicId { get; set; }

    /// <summary>
    /// Gets or sets the user id.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the ranked entries.
    /// </summary>
    public IReadOnlyList<RankingEntry> Entries { get; set; } = [];

    /// <summary>
    /// Gets or sets the last update time (UTC).
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
