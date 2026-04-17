using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.OnePace;

/// <summary>
/// Custom metadata provider for One Pace arcs (seasons).
/// Runs after all remote providers, giving it the final word on metadata.
/// </summary>
public class ArcProvider : ICustomMetadataProvider<Season>
{
    private readonly IRepository _repository;
    private readonly ILogger<ArcProvider> _logger;

    public ArcProvider(IRepository repository, ILogger<ArcProvider> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public string Name => Plugin.ProviderName;

    public async Task<ItemUpdateType> FetchAsync(Season item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        _logger.LogInformation("One Pace ArcProvider.FetchAsync called for: Name={Name}, Path={Path}, IndexNumber={Index}", item.Name, item.Path, item.IndexNumber);

        var info = new SeasonInfo { Path = item.Path, Name = item.Name };
        IdentifierUtil.CopyProviderIds(item, info);

        var arc = await ArcIdentifier.IdentifyAsync(_repository, info, cancellationToken).ConfigureAwait(false);
        if (arc == null)
        {
            _logger.LogDebug("One Pace ArcProvider: not identified, skipping");
            return ItemUpdateType.None;
        }

        item.Name = arc.InvariantTitle;
        item.Overview = arc.Description;
        item.IndexNumber = arc.Rank;
        item.PremiereDate = arc.ReleaseDate;
        item.ProductionYear = arc.ReleaseDate?.Year;
        item.SortName = arc.Rank.ToString("D3", CultureInfo.InvariantCulture) + " " + arc.InvariantTitle;

        // Remove ALL remote provider IDs so their image providers can't download wrong art
        item.ProviderIds = new Dictionary<string, string>();
        item.SetOnePaceId(arc.Id);

        // Clear all existing images (removes wrong art from other providers)
        item.ImageInfos = Array.Empty<ItemImageInfo>();

        // Set season poster from plugin data folder
        var seasonFolder = arc.Rank == 0 ? "Specials" : $"Season {arc.Rank}";
        var posterPath = Path.Combine(WebRepository.GetDataPath(), seasonFolder, "poster.png");
        if (File.Exists(posterPath))
        {
            item.SetImagePath(ImageType.Primary, posterPath);
            _logger.LogInformation("One Pace set season poster: {Path}", posterPath);
        }

        _logger.LogInformation("One Pace custom provider set arc metadata: {Title} (Rank: {Rank})", item.Name, arc.Rank);
        return ItemUpdateType.MetadataEdit;
    }
}
