using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.OnePace.Configuration;

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
        PreferredMetadataLanguage = "en";
        LibraryPath = string.Empty;
    }

    /// <summary>
    /// Gets or sets the preferred metadata language code.
    /// </summary>
    public string PreferredMetadataLanguage { get; set; }

    /// <summary>
    /// Gets or sets the path to the One Pace library folder.
    /// When set, any item under this path will be treated as One Pace content.
    /// </summary>
    public string LibraryPath { get; set; }
}
