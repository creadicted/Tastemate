namespace Jellyfin.Plugin.Tastemate.Api;

/// <summary>
/// Request to ask Seerr for a movie.
/// </summary>
/// <param name="TmdbId">The TMDb id.</param>
public record SeerrRequestBody(string TmdbId);
