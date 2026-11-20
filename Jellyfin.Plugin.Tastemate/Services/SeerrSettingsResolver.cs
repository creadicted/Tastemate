using System;
using System.Linq;
using MediaBrowser.Common.Plugins;

namespace Jellyfin.Plugin.Tastemate.Services;

/// <summary>
/// Finds the Seerr settings: Tastemate's own, or those of the Jellyfin Enhanced plugin when it is installed.
/// </summary>
public class SeerrSettingsResolver
{
    private static readonly Guid JellyfinEnhancedId = new("f69e946a-4b3c-4e9a-8f0a-8d7c1b2c4d9b");

    private readonly IPluginManager _pluginManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeerrSettingsResolver"/> class.
    /// </summary>
    /// <param name="pluginManager">Instance of the <see cref="IPluginManager"/> interface.</param>
    public SeerrSettingsResolver(IPluginManager pluginManager)
    {
        _pluginManager = pluginManager;
    }

    /// <summary>
    /// Gets a value indicating whether the Jellyfin Enhanced plugin is installed and active.
    /// </summary>
    public bool JellyfinEnhancedInstalled => GetJellyfinEnhancedConfiguration() is not null;

    /// <summary>
    /// Gets the settings to use. Tastemate's own settings win over Jellyfin Enhanced's.
    /// </summary>
    /// <returns>The settings, or null when Seerr is not set up anywhere.</returns>
    public SeerrConnection? Resolve()
    {
        var config = Plugin.Instance?.Configuration;
        if (config is not null && !string.IsNullOrWhiteSpace(config.SeerrAddress) && !string.IsNullOrWhiteSpace(config.SeerrApiKey))
        {
            return new SeerrConnection(config.SeerrAddress, config.SeerrApiKey, "plugin");
        }

        var enhanced = GetJellyfinEnhancedConfiguration();
        if (enhanced is null || ReadProperty(enhanced, "JellyseerrEnabled") is not true)
        {
            return null;
        }

        var address = (ReadProperty(enhanced, "JellyseerrUrls") as string)?
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        var apiKey = (ReadProperty(enhanced, "JellyseerrApiKey") as string)?.Trim();
        if (string.IsNullOrEmpty(address) || string.IsNullOrEmpty(apiKey)
            || !Uri.TryCreate(address, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        {
            return null;
        }

        return new SeerrConnection(address, apiKey, "jellyfin-enhanced");
    }

    private static object? ReadProperty(object target, string name)
    {
        return target.GetType().GetProperty(name)?.GetValue(target);
    }

    // The other plugin's types are not referenced, so its configuration is read by property name.
    private object? GetJellyfinEnhancedConfiguration()
    {
        var plugin = _pluginManager.Plugins.FirstOrDefault(p => p.Id == JellyfinEnhancedId && p.Instance is not null);
        return plugin?.Instance is IHasPluginConfiguration configured ? configured.Configuration : null;
    }
}
