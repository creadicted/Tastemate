using System;
using System.Collections.Generic;
using Jellyfin.Plugin.Tastemate.Models;

namespace Jellyfin.Plugin.Tastemate.Api;

/// <summary>
/// Request to replace the caller's ranking.
/// </summary>
/// <param name="Entries">The ranked entries.</param>
public record SetRankingRequest(IReadOnlyList<RankingEntry> Entries);
