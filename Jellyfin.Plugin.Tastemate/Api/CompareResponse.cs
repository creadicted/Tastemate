using System.Collections.Generic;
using Jellyfin.Plugin.Tastemate.Models;

namespace Jellyfin.Plugin.Tastemate.Api;

/// <summary>
/// A comparison with display data.
/// </summary>
/// <param name="UserAName">The caller's name.</param>
/// <param name="UserBName">The other user's name.</param>
/// <param name="Result">The comparison.</param>
/// <param name="Movies">Display data by movie key.</param>
public record CompareResponse(string UserAName, string UserBName, ComparisonResult Result, IReadOnlyDictionary<string, MovieDto> Movies);
