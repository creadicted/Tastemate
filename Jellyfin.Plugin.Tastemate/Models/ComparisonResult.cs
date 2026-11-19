using System.Collections.Generic;

namespace Jellyfin.Plugin.Tastemate.Models;

/// <summary>
/// The result of comparing two rankings.
/// </summary>
public class ComparisonResult
{
    /// <summary>
    /// Gets or sets the movies in both lists, ordered by rank in the first list.
    /// </summary>
    public IReadOnlyList<SharedEntry> Shared { get; set; } = [];

    /// <summary>
    /// Gets or sets the movies only in the first list.
    /// </summary>
    public IReadOnlyList<RankingEntry> OnlyInA { get; set; } = [];

    /// <summary>
    /// Gets or sets the movies only in the second list.
    /// </summary>
    public IReadOnlyList<RankingEntry> OnlyInB { get; set; } = [];

    /// <summary>
    /// Gets or sets the similarity from 0 (nothing in common) to 100 (identical lists).
    /// </summary>
    public double Similarity { get; set; }
}
