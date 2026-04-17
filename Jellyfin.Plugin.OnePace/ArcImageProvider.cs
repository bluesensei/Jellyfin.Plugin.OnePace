using System.Collections.Generic;
using System.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;

namespace Jellyfin.Plugin.OnePace;

/// <summary>
/// Local image provider for One Pace arcs (seasons).
/// Local images take priority over remote image providers.
/// </summary>
public class ArcImageProvider : ILocalImageProvider
{
    public string Name => Plugin.ProviderName;

    public bool Supports(BaseItem item)
    {
        if (item is not Season) return false;
        if (item.GetOnePaceId() != null) return true;
        if (item.Path != null && IdentifierUtil.OnePaceInvariantTitleRegex.IsMatch(item.Path)) return true;
        return IdentifierUtil.IsUnderLibraryPath(item.Path);
    }

    public IEnumerable<LocalImageInfo> GetImages(BaseItem item, IDirectoryService directoryService)
    {
        var list = new List<LocalImageInfo>();
        var arcId = item.GetOnePaceId();
        if (arcId == null) return list;

        var seasonFolder = arcId == "0" ? "Specials" : $"Season {arcId}";
        var posterPath = Path.Combine(WebRepository.GetDataPath(), seasonFolder, "poster.png");

        if (File.Exists(posterPath))
        {
            list.Add(new LocalImageInfo
            {
                FileInfo = new FileSystemMetadata
                {
                    FullName = posterPath,
                    Name = "poster.png",
                    Exists = true,
                    IsDirectory = false,
                },
                Type = ImageType.Primary,
            });
        }

        return list;
    }
}
