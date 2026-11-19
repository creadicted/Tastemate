using System;
using System.Collections.Generic;
using Jellyfin.Plugin.TopCompare.Models;

namespace Jellyfin.Plugin.TopCompare.Api;

/// <summary>
/// A comparison with display data.
/// </summary>
/// <param name="UserAName">The caller's name.</param>
/// <param name="UserBName">The other user's name.</param>
/// <param name="Result">The comparison.</param>
/// <param name="Names">Movie names by item id.</param>
public record CompareResponse(string UserAName, string UserBName, ComparisonResult Result, IReadOnlyDictionary<Guid, string> Names);
