using System;

namespace Jellyfin.Plugin.Tastemate.Api;

/// <summary>
/// Display data of a movie.
/// </summary>
/// <param name="Name">The movie name.</param>
/// <param name="Year">The production year.</param>
/// <param name="ItemId">The library item id, if the movie is in the library.</param>
/// <param name="Poster">The poster image address of an external movie.</param>
public record MovieDto(string Name, int? Year, Guid? ItemId, string? Poster);
