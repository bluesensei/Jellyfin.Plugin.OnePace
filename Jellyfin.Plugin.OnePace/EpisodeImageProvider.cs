using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;

namespace Jellyfin.Plugin.OnePace;

/// <summary>
/// Local image provider for One Pace episodes.
/// Currently a no-op since there are no per-episode images.
/// </summary>
public class EpisodeImageProvider : ILocalImageProvider
{
    public string Name => Plugin.ProviderName;

    public bool Supports(BaseItem item) => false;

    public IEnumerable<LocalImageInfo> GetImages(BaseItem item, IDirectoryService directoryService)
    {
        return [];
    }
}
