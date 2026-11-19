using System;
using System.Collections.Generic;
using Jellyfin.Plugin.Tastemate.Models;

namespace Jellyfin.Plugin.Tastemate.Api;

/// <summary>
/// A ranked movie with display data.
/// </summary>
/// <param name="Rank">The 1-based rank.</param>
/// <param name="ItemId">The library item id.</param>
/// <param name="Name">The movie name.</param>
public record RankedEntryDto(int Rank, Guid ItemId, string Name);
