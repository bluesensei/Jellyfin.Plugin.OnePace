using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
/// Custom metadata provider for One Pace episodes.
/// Runs after all remote providers, giving it the final word on metadata.
/// </summary>
public class EpisodeProvider : ICustomMetadataProvider<Episode>
{
    private readonly IRepository _repository;
    private readonly ILogger<EpisodeProvider> _logger;

    public EpisodeProvider(IRepository repository, ILogger<EpisodeProvider> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public string Name => Plugin.ProviderName;

    public async Task<ItemUpdateType> FetchAsync(Episode item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        _logger.LogInformation("One Pace EpisodeProvider.FetchAsync called for: Name={Name}, Path={Path}", item.Name, item.Path);

        var info = new EpisodeInfo { Path = item.Path, Name = item.Name };
        IdentifierUtil.CopyProviderIds(item, info);

        var episode = await EpisodeIdentifier.IdentifyAsync(_repository, info, cancellationToken).ConfigureAwait(false);
        if (episode == null)
        {
            _logger.LogDebug("One Pace EpisodeProvider: not identified, skipping");
            return ItemUpdateType.None;
        }

        var arcs = await _repository.FindAllArcsAsync(cancellationToken).ConfigureAwait(false);
        var arc = arcs.FirstOrDefault(a => string.Equals(a.Id, episode.ArcId, StringComparison.Ordinal));

        item.Name = episode.InvariantTitle;
        item.Overview = episode.Description;
        item.IndexNumber = episode.Rank;
        item.ParentIndexNumber = arc?.Rank;
        item.PremiereDate = episode.ReleaseDate;
        item.ProductionYear = episode.ReleaseDate?.Year;
        item.SortName = (arc?.Rank ?? 0).ToString("D3", CultureInfo.InvariantCulture)
            + "E" + episode.Rank.ToString("D2", CultureInfo.InvariantCulture);

        // Remove ALL remote provider IDs so their image providers can't download wrong art
        item.ProviderIds = new Dictionary<string, string>();
        item.SetOnePaceId(episode.Id);

        // Clear all existing images (removes wrong art from other providers)
        item.ImageInfos = Array.Empty<ItemImageInfo>();

        // Set episode image to season poster (no per-episode art available)
        if (arc != null)
        {
            var seasonFolder = arc.Rank == 0 ? "Specials" : $"Season {arc.Rank}";
            var posterPath = Path.Combine(WebRepository.GetDataPath(), seasonFolder, "poster.png");
            if (File.Exists(posterPath))
            {
                item.SetImagePath(ImageType.Primary, posterPath);
            }
        }

        _logger.LogInformation(
            "One Pace custom provider set episode metadata: {Title} (S{Season:D2}E{Episode:D2})",
            item.Name,
            arc?.Rank ?? 0,
            episode.Rank);
        return ItemUpdateType.MetadataEdit;
    }
}
