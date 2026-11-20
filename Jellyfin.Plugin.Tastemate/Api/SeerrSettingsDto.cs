namespace Jellyfin.Plugin.Tastemate.Api;

/// <summary>
/// The Seerr settings. The API key is never returned.
/// </summary>
/// <param name="Address">The Seerr address.</param>
/// <param name="HasApiKey">Whether an API key is stored.</param>
/// <param name="Source">Where the active settings come from: plugin, jellyfin-enhanced or none.</param>
/// <param name="EnhancedInstalled">Whether the Jellyfin Enhanced plugin is installed.</param>
public record SeerrSettingsDto(string Address, bool HasApiKey, string Source, bool EnhancedInstalled);
