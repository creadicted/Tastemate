namespace Jellyfin.Plugin.Tastemate.Services;

/// <summary>
/// The address and API key used to reach Seerr.
/// </summary>
/// <param name="Address">The Seerr address.</param>
/// <param name="ApiKey">The API key.</param>
/// <param name="Source">Where the settings come from: <c>plugin</c> or <c>jellyfin-enhanced</c>.</param>
public record SeerrConnection(string Address, string ApiKey, string Source);
