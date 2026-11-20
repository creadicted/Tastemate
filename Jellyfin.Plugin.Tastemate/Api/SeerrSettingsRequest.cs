namespace Jellyfin.Plugin.Tastemate.Api;

/// <summary>
/// Request to change the Seerr settings.
/// </summary>
/// <param name="Address">The Seerr address. Empty disables the integration.</param>
/// <param name="ApiKey">The new API key. Empty keeps the stored key.</param>
public record SeerrSettingsRequest(string? Address, string? ApiKey);
