namespace Jellyfin.Plugin.OnePace.Model;

/// <summary>
/// Represents artwork for a series, arc, or episode.
/// </summary>
public interface IArt
{
    /// <summary>
    /// Gets the URL of the artwork.
    /// </summary>
    string Url { get; }

    /// <summary>
    /// Gets the width in pixels.
    /// </summary>
    int? Width { get; }

    /// <summary>
    /// Gets the height in pixels.
    /// </summary>
    int? Height { get; }
}
