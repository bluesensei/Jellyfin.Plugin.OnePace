using System.Collections.Generic;
using System.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;

namespace Jellyfin.Plugin.OnePace;

/// <summary>
/// Local image provider for One Pace series.
/// Local images take priority over remote image providers.
/// </summary>
public class SeriesImageProvider : ILocalImageProvider
{
    public string Name => Plugin.ProviderName;

    public bool Supports(BaseItem item)
    {
        if (item is not Series) return false;
        if (item.GetOnePaceId() != null) return true;
        if (item.Path != null && IdentifierUtil.OnePaceInvariantTitleRegex.IsMatch(item.Path)) return true;
        return IdentifierUtil.IsUnderLibraryPath(item.Path);
    }

    public IEnumerable<LocalImageInfo> GetImages(BaseItem item, IDirectoryService directoryService)
    {
        var list = new List<LocalImageInfo>();
        var dataPath = WebRepository.GetDataPath();

        TryAddImage(list, Path.Combine(dataPath, "poster.png"), ImageType.Primary);
        TryAddImage(list, Path.Combine(dataPath, "logo.png"), ImageType.Logo);
        TryAddImage(list, Path.Combine(dataPath, "backdrop.jpg"), ImageType.Backdrop);

        return list;
    }

    private static void TryAddImage(List<LocalImageInfo> list, string path, ImageType type)
    {
        if (!File.Exists(path)) return;

        list.Add(new LocalImageInfo
        {
            FileInfo = new FileSystemMetadata
            {
                FullName = path,
                Name = Path.GetFileName(path),
                Exists = true,
                IsDirectory = false,
            },
            Type = type,
        });
    }
}
