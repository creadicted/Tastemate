using System;

namespace Jellyfin.Plugin.Tastemate.Models;

/// <summary>
/// One ranked movie. It is either a library item (<see cref="ItemId"/> set) or an external
/// movie that only exists on TMDb. Movies are matched between users by TMDb id when known.
/// </summary>
public class RankingEntry
{
    /// <summary>
    /// Gets or sets the 1-based rank.
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// Gets or sets the library item id of the movie, or <see cref="Guid.Empty"/> for an external movie.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the TMDb id of the movie.
    /// </summary>
    public string? TmdbId { get; set; }

    /// <summary>
    /// Gets or sets the movie title as stored when the ranking was saved.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the production year.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Gets or sets the poster image address of an external movie.
    /// </summary>
    public string? Poster { get; set; }

    /// <summary>
    /// Gets the key used to match the same movie across users and sources.
    /// </summary>
    public string Key => string.IsNullOrEmpty(TmdbId)
        ? "item:" + ItemId.ToString("N")
        : "tmdb:" + TmdbId;
}
