namespace Jellyfin.Plugin.Tastemate.Api;

/// <summary>
/// A movie found by the metadata provider search.
/// </summary>
/// <param name="TmdbId">The TMDb id.</param>
/// <param name="Name">The movie name.</param>
/// <param name="Year">The production year.</param>
/// <param name="Poster">The poster image address.</param>
public record SearchResultDto(string TmdbId, string Name, int? Year, string? Poster);
