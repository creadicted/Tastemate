using System.Collections.Generic;

namespace Jellyfin.Plugin.Tastemate.Api;

/// <summary>
/// The Seerr state of some movies.
/// </summary>
/// <param name="Enabled">Whether Seerr is configured.</param>
/// <param name="States">The state per TMDb id: none, pending, processing, partial or available.</param>
public record SeerrStatusResponse(bool Enabled, IReadOnlyDictionary<string, string> States);
