using System;
using System.Collections.Generic;
using Jellyfin.Plugin.Tastemate.Models;

namespace Jellyfin.Plugin.Tastemate.Api;

/// <summary>
/// A user who ranked a topic.
/// </summary>
/// <param name="UserId">The user id.</param>
/// <param name="UserName">The user name.</param>
/// <param name="EntryCount">The number of ranked movies.</param>
public record RankerDto(Guid UserId, string UserName, int EntryCount);
