using System;

namespace Jellyfin.Plugin.Tastemate.Models;

/// <summary>
/// A topic users can rank movies for, e.g. "Top 10 Horror Movies".
/// </summary>
public class Topic
{
    /// <summary>
    /// Gets or sets the topic id.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the topic title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of entries in the list (the x in "Top x").
    /// </summary>
    public int Size { get; set; }

    /// <summary>
    /// Gets or sets the id of the user who created the topic.
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the creation time (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
