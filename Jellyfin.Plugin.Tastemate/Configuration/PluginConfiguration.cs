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
        SeerrAddress = string.Empty;
        SeerrApiKey = string.Empty;
    }

    /// <summary>
    /// Gets or sets the address of the Seerr server, for example http://seerr:5055. Empty disables the Seerr integration.
    /// </summary>
    public string SeerrAddress { get; set; }

    /// <summary>
    /// Gets or sets the Seerr API key.
    /// </summary>
    public string SeerrApiKey { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of entries a topic may have.
    /// </summary>
    public int MaxTopSize { get; set; }
}
