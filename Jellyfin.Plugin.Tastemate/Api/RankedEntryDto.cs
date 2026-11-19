using System;

namespace Jellyfin.Plugin.Tastemate.Api;

/// <summary>
/// A ranked movie with display data.
/// </summary>
/// <param name="Rank">The 1-based rank.</param>
/// <param name="ItemId">The library item id, or empty for an external movie.</param>
/// <param name="TmdbId">The TMDb id.</param>
/// <param name="Name">The movie name.</param>
/// <param name="Year">The production year.</param>
/// <param name="Poster">The poster image address of an external movie.</param>
public record RankedEntryDto(int Rank, Guid ItemId, string? TmdbId, string Name, int? Year, string? Poster);
