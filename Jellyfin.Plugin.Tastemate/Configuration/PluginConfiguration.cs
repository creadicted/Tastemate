using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Tastemate.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        MaxTopSize = 25;
    }

    /// <summary>
    /// Gets or sets the maximum number of entries a topic may have.
    /// </summary>
    public int MaxTopSize { get; set; }
}
