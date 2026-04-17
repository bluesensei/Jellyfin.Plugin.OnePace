using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.OnePace.Model;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.OnePace;

internal static class EpisodeIdentifier
{
    public static async Task<IEpisode?> IdentifyAsync(
        IRepository repository,
        ItemLookupInfo itemLookupInfo,
        CancellationToken cancellationToken)
    {
        // First, try matching by stored provider ID
        var episodeId = itemLookupInfo.GetOnePaceId();
        if (episodeId != null)
        {
            var episodeInfo = await repository
                .FindEpisodeByIdAsync(episodeId, cancellationToken)
                .ConfigureAwait(false);
            if (episodeInfo != null)
            {
                return episodeInfo;
            }
        }

        if (itemLookupInfo.Path == null ||
            (!IdentifierUtil.OnePaceInvariantTitleRegex.IsMatch(itemLookupInfo.Path)
             && !IdentifierUtil.IsUnderLibraryPath(itemLookupInfo.Path)))
        {
            return null;
        }

        var episodes = await repository.FindAllEpisodesAsync(cancellationToken).ConfigureAwait(false);
        var arcs = await repository.FindAllArcsAsync(cancellationToken).ConfigureAwait(false);
        var fileName = Path.GetFileNameWithoutExtension(itemLookupInfo.Path);

        // Parse the filename to extract arc name and episode number
        var parsed = OnePaceFileParser.ParseEpisodeFileName(fileName);

        // Strategy 1: Use parsed arc name + episode number to find the exact episode
        if (parsed?.ArcName != null && parsed.EpisodeNumber != null)
        {
            var ep = FindByArcNameAndRank(arcs, episodes, parsed.ArcName, parsed.EpisodeNumber.Value);
            if (ep != null)
            {
                return ep;
            }
        }

        // Strategy 2: Exact chapter range match (only compare extracted chapter range, not broad regex)
        if (parsed?.ChapterRange != null)
        {
            foreach (var episode in episodes)
            {
                if (!string.IsNullOrEmpty(episode.MangaChapters) &&
                    string.Equals(episode.MangaChapters, parsed.ChapterRange, StringComparison.OrdinalIgnoreCase))
                {
                    return episode;
                }
            }
        }

        // Strategy 3: Match episode invariant title against filename
        // Only match if the episode title is specific enough (more than 2 chars)
        foreach (var episode in episodes.OrderByDescending(e => e.InvariantTitle.Length))
        {
            if (episode.InvariantTitle.Length > 2 &&
                IdentifierUtil.BuildTextRegex(episode.InvariantTitle).IsMatch(fileName))
            {
                return episode;
            }
        }

        // Strategy 4: Parent folder name as arc context + episode number from filename
        var parentDir = Path.GetDirectoryName(itemLookupInfo.Path);
        var parentFolderName = parentDir != null ? Path.GetFileName(parentDir) : null;
        if (parentFolderName != null)
        {
            int? epNum = parsed?.EpisodeNumber;
            if (epNum == null)
            {
                var numMatch = Regex.Match(fileName, @"(\d+)");
                if (numMatch.Success && int.TryParse(numMatch.Groups[1].Value, out int n))
                {
                    epNum = n;
                }
            }

            if (epNum != null)
            {
                var ep = FindByArcNameAndRank(arcs, episodes, parentFolderName, epNum.Value);
                if (ep != null)
                {
                    return ep;
                }
            }
        }

        return null;
    }

    private static IEpisode? FindByArcNameAndRank(
        System.Collections.Generic.IReadOnlyCollection<IArc> arcs,
        System.Collections.Generic.IReadOnlyCollection<IEpisode> episodes,
        string arcNameQuery,
        int episodeRank)
    {
        foreach (var arc in arcs.OrderByDescending(a => a.InvariantTitle.Length))
        {
            if (IdentifierUtil.BuildTextRegex(arc.InvariantTitle).IsMatch(arcNameQuery))
            {
                var ep = episodes.FirstOrDefault(e =>
                    string.Equals(e.ArcId, arc.Id, StringComparison.Ordinal)
                    && e.Rank == episodeRank);
                if (ep != null)
                {
                    return ep;
                }
            }
        }

        return null;
    }
}
