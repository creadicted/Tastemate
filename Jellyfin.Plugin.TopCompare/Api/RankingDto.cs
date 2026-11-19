using System;
using System.Collections.Generic;
using Jellyfin.Plugin.TopCompare.Models;

namespace Jellyfin.Plugin.TopCompare.Api;

/// <summary>
/// A user's ranking with display data.
/// </summary>
/// <param name="UserId">The user id.</param>
/// <param name="UserName">The user name.</param>
/// <param name="Entries">The ranked entries.</param>
public record RankingDto(Guid UserId, string UserName, IReadOnlyList<RankedEntryDto> Entries);
