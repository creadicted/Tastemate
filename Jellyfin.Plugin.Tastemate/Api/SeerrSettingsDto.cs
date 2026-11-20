namespace Jellyfin.Plugin.Tastemate.Api;

/// <summary>
/// The Seerr settings. The API key is never returned.
/// </summary>
/// <param name="Address">The Seerr address.</param>
/// <param name="HasApiKey">Whether an API key is stored.</param>
public record SeerrSettingsDto(string Address, bool HasApiKey);
