using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.OnePace;

/// <summary>
/// Custom metadata provider for One Pace series.
/// Runs after all remote providers, giving it the final word on metadata.
/// </summary>
public class SeriesProvider : ICustomMetadataProvider<Series>
{
    private readonly IRepository _repository;
    private readonly ILogger<SeriesProvider> _logger;

    public SeriesProvider(IRepository repository, ILogger<SeriesProvider> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public string Name => Plugin.ProviderName;

    public async Task<ItemUpdateType> FetchAsync(Series item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        _logger.LogInformation("One Pace SeriesProvider.FetchAsync called for: Name={Name}, Path={Path}", item.Name, item.Path);

        var info = new SeriesInfo { Path = item.Path, Name = item.Name };
        IdentifierUtil.CopyProviderIds(item, info);

        var series = await SeriesIdentifier.IdentifyAsync(_repository, info, cancellationToken).ConfigureAwait(false);
        if (series == null)
        {
            _logger.LogDebug("One Pace SeriesProvider: not identified, skipping");
            return ItemUpdateType.None;
        }

        item.Name = series.InvariantTitle;
        item.OriginalTitle = series.OriginalTitle;
        item.Overview = series.Description;
        item.PremiereDate = new DateTime(1999, 10, 20, 0, 0, 0, DateTimeKind.Utc);
        item.ProductionYear = 1999;
        item.Genres = new[] { "Animation", "Anime", "Action", "Adventure" };
        item.OfficialRating = "TV-14";
        item.CommunityRating = null;
        item.SortName = "One Pace";
        item.Tags = Array.Empty<string>();
        item.Studios = Array.Empty<string>();
        item.AirTime = null;
        item.AirDays = Array.Empty<DayOfWeek>();
        item.Status = null;

        // Remove ALL remote provider IDs so their image providers can't download wrong art
        item.ProviderIds = new Dictionary<string, string>();
        item.SetOnePaceId(Plugin.DummySeriesId);

        // Clear all existing images (removes wrong art from TMDB/AniList/etc.)
        item.ImageInfos = Array.Empty<ItemImageInfo>();

        // Set images from plugin data folder
        var dataPath = WebRepository.GetDataPath();
        SetImageIfExists(item, Path.Combine(dataPath, "poster.png"), ImageType.Primary);
        SetImageIfExists(item, Path.Combine(dataPath, "logo.png"), ImageType.Logo);

        // Add all available backdrops
        SetImageIfExists(item, Path.Combine(dataPath, "backdrop.jpg"), ImageType.Backdrop);
        for (int i = 2; i <= 10; i++)
        {
            var extraBackdrop = Path.Combine(dataPath, $"backdrop-{i}.jpg");
            if (File.Exists(extraBackdrop))
            {
                item.SetImagePath(ImageType.Backdrop, i - 1, new MediaBrowser.Model.IO.FileSystemMetadata
                {
                    FullName = extraBackdrop,
                    Name = Path.GetFileName(extraBackdrop),
                    Exists = true,
                    IsDirectory = false,
                });
                _logger.LogInformation("One Pace set Backdrop image [{Index}]: {Path}", i - 1, extraBackdrop);
            }
            else
            {
                break;
            }
        }

        _logger.LogInformation("One Pace custom provider set series metadata: {Title}", item.Name);
        return ItemUpdateType.MetadataEdit;
    }

    private void SetImageIfExists(BaseItem item, string path, ImageType type)
    {
        if (File.Exists(path))
        {
            item.SetImagePath(type, path);
            _logger.LogInformation("One Pace set {Type} image: {Path}", type, path);
        }
        else
        {
            _logger.LogWarning("One Pace image not found: {Path}", path);
        }
    }
}
