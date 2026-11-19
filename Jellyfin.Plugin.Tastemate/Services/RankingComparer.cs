using System;
using System.Linq;
using Jellyfin.Plugin.Tastemate.Models;

namespace Jellyfin.Plugin.Tastemate.Services;

/// <summary>
/// Compares two rankings.
/// </summary>
public static class RankingComparer
{
    /// <summary>
    /// Compares two rankings of the same topic.
    /// Every shared movie scores 1 - |rankA - rankB| / size, the sum is divided by size.
    /// Movies in only one list score 0.
    /// </summary>
    /// <param name="a">The first ranking.</param>
    /// <param name="b">The second ranking.</param>
    /// <param name="size">The topic size.</param>
    /// <returns>The comparison.</returns>
    public static ComparisonResult Compare(UserRanking a, UserRanking b, int size)
    {
        var ranksA = a.Entries.ToDictionary(e => e.Key, e => e.Rank);
        var ranksB = b.Entries.ToDictionary(e => e.Key, e => e.Rank);

        var shared = a.Entries
            .Where(e => ranksB.ContainsKey(e.Key))
            .OrderBy(e => e.Rank)
            .Select(e => new SharedEntry
            {
                Key = e.Key,
                RankA = e.Rank,
                RankB = ranksB[e.Key],
                Difference = Math.Abs(e.Rank - ranksB[e.Key])
            })
            .ToList();

        var score = shared.Sum(s => 1d - ((double)s.Difference / size));

        return new ComparisonResult
        {
            Shared = shared,
            OnlyInA = a.Entries.Where(e => !ranksB.ContainsKey(e.Key)).OrderBy(e => e.Rank).ToList(),
            OnlyInB = b.Entries.Where(e => !ranksA.ContainsKey(e.Key)).OrderBy(e => e.Rank).ToList(),
            Similarity = Math.Round(score / size * 100d, 1)
        };
    }
}
