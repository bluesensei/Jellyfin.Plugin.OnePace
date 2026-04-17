using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.OnePace;

/// <summary>
/// External ID entry for One Pace series.
/// </summary>
public class SeriesExternalId : IExternalId
{
    /// <inheritdoc />
    public string ProviderName => Plugin.ProviderName;

    /// <inheritdoc />
    public string Key => Plugin.ProviderName;

    /// <inheritdoc />
    public ExternalIdMediaType? Type => ExternalIdMediaType.Series;

    /// <inheritdoc />
    public string? UrlFormatString => null;

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item)
    {
        return item is Series;
    }
}
