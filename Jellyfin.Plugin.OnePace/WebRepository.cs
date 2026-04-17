using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Jellyfin.Plugin.OnePace.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.OnePace;

/// <summary>
/// Provides One Pace metadata from local NFO files in the plugin's "One Pace" subfolder.
/// </summary>
public class WebRepository : IRepository
{
    private const string DataFolderName = "One Pace";
    private const string CacheKey = "OnePaceMetadata";

    private static readonly Regex MangaChapterRegex = new(
        @"Manga Chapter\(s\):\s*(.+?)$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex AnimeEpisodeRegex = new(
        @"Anime Episode\(s\):\s*(.+?)$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex SeasonFolderRegex = new(
        @"^Season (\d+)$",
        RegexOptions.Compiled);

    private static readonly Regex LeadingNumberRegex = new(
        @"^\d+\.\s*",
        RegexOptions.Compiled);

    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<WebRepository> _log;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebRepository"/> class.
    /// </summary>
    public WebRepository(IMemoryCache memoryCache, ILogger<WebRepository> logger)
    {
        _memoryCache = memoryCache;
        _log = logger;
    }

    /// <summary>
    /// Gets the path to the "One Pace" data folder inside the plugin directory.
    /// </summary>
    internal static string GetDataPath()
    {
        var assemblyDir = Path.GetDirectoryName(typeof(WebRepository).Assembly.Location)
                          ?? throw new InvalidOperationException("Cannot determine plugin assembly directory");
        return Path.Combine(assemblyDir, DataFolderName);
    }

    /// <summary>
    /// Returns an HttpResponseMessage wrapping a local file, for use by image providers.
    /// </summary>
    internal static HttpResponseMessage CreateLocalFileResponse(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        var bytes = File.ReadAllBytes(filePath);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(bytes)
        };

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            ext switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                _ => "application/octet-stream"
            });

        return response;
    }

    private Task<OnePaceData?> LoadMetadataAsync()
    {
        return _memoryCache.GetOrCreateAsync(CacheKey, cacheEntry =>
        {
            cacheEntry.SlidingExpiration = TimeSpan.FromHours(6);

            var dataPath = GetDataPath();
            if (!Directory.Exists(dataPath))
            {
                _log.LogWarning("One Pace data folder not found at {Path}. Copy the 'One Pace' folder from one-pace-jellyfin into the plugin directory.", dataPath);
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                return Task.FromResult<OnePaceData?>(null);
            }

            try
            {
                var data = ParseLocalFolder(dataPath);
                _log.LogInformation(
                    "Loaded One Pace metadata from disk: {ArcCount} arcs, {EpisodeCount} episodes",
                    data.Arcs.Count,
                    data.Episodes.Count);
                return Task.FromResult<OnePaceData?>(data);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Failed to parse One Pace metadata from {Path}", dataPath);
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                return Task.FromResult<OnePaceData?>(null);
            }
        })!;
    }

    private OnePaceData ParseLocalFolder(string rootPath)
    {
        // Parse tvshow.nfo for series description
        string? seriesPlot = null;
        var tvshowNfo = Path.Combine(rootPath, "tvshow.nfo");
        if (File.Exists(tvshowNfo))
        {
            var doc = XDocument.Load(tvshowNfo);
            seriesPlot = doc.Root?.Element("plot")?.Value;
        }

        var series = new OnePaceSeries { Description = seriesPlot };

        var arcs = new List<OnePaceArc>();
        var episodes = new List<OnePaceEpisode>();
        var arcPosterPaths = new Dictionary<string, string?>();

        // Enumerate Season N and Specials folders
        foreach (var folderPath in Directory.GetDirectories(rootPath))
        {
            var folderName = Path.GetFileName(folderPath);

            int seasonNumber;
            if (string.Equals(folderName, "Specials", StringComparison.OrdinalIgnoreCase))
            {
                seasonNumber = 0;
            }
            else
            {
                var match = SeasonFolderRegex.Match(folderName);
                if (!match.Success)
                {
                    continue;
                }

                seasonNumber = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            }

            var arcId = seasonNumber.ToString(CultureInfo.InvariantCulture);
            var arcTitle = folderName;

            // Parse season.nfo for arc title
            var seasonNfo = Path.Combine(folderPath, "season.nfo");
            if (File.Exists(seasonNfo))
            {
                var doc = XDocument.Load(seasonNfo);
                var rawTitle = doc.Root?.Element("title")?.Value;
                if (rawTitle != null)
                {
                    arcTitle = LeadingNumberRegex.Replace(rawTitle, string.Empty);
                }
            }

            // Poster path
            var posterPath = Path.Combine(folderPath, "poster.png");
            arcPosterPaths[arcId] = File.Exists(posterPath) ? posterPath : null;

            // Parse episode NFOs
            var nfoFiles = Directory.GetFiles(folderPath, "*.nfo")
                .Where(f => !Path.GetFileName(f).Equals("season.nfo", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase);

            var arcEpisodeList = new List<OnePaceEpisode>();

            foreach (var nfoFile in nfoFiles)
            {
                try
                {
                    var doc = XDocument.Load(nfoFile);
                    var root = doc.Root;
                    if (root == null)
                    {
                        continue;
                    }

                    var epTitle = root.Element("title")?.Value ?? "Unknown";
                    var epPlot = root.Element("plot")?.Value;
                    var epNumber = int.TryParse(root.Element("episode")?.Value, out int ep) ? ep : 0;
                    var premiered = root.Element("premiered")?.Value;

                    DateTime? releaseDate = null;
                    if (DateTime.TryParse(premiered, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    {
                        releaseDate = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                    }

                    var mangaChapters = ExtractFromPlot(epPlot, MangaChapterRegex);
                    var animeEpisodes = ExtractFromPlot(epPlot, AnimeEpisodeRegex);
                    var cleanPlot = CleanPlot(epPlot);

                    var episodeId = FormattableString.Invariant($"S{seasonNumber}E{epNumber}");

                    arcEpisodeList.Add(new OnePaceEpisode
                    {
                        Id = episodeId,
                        ArcId = arcId,
                        Rank = epNumber,
                        InvariantTitle = epTitle,
                        Description = cleanPlot,
                        MangaChapters = mangaChapters,
                        ReleaseDate = releaseDate,
                        AnimeEpisodes = animeEpisodes
                    });
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Failed to parse NFO: {Path}", nfoFile);
                }
            }

            // Derive arc release date from earliest episode
            DateTime? arcReleaseDate = arcEpisodeList
                .Where(e => e.ReleaseDate.HasValue)
                .OrderBy(e => e.ReleaseDate)
                .FirstOrDefault()?.ReleaseDate;

            arcs.Add(new OnePaceArc
            {
                Id = arcId,
                Rank = seasonNumber,
                InvariantTitle = arcTitle,
                Description = null,
                MangaChapters = null,
                ReleaseDate = arcReleaseDate,
                Saga = null
            });

            episodes.AddRange(arcEpisodeList);
        }

        arcs.Sort((a, b) => a.Rank.CompareTo(b.Rank));

        // Series poster and logo
        var seriesPoster = Path.Combine(rootPath, "poster.png");
        var seriesLogo = Path.Combine(rootPath, "logo.png");

        return new OnePaceData
        {
            Series = series,
            Arcs = arcs,
            Episodes = episodes,
            ArcPosterPaths = arcPosterPaths,
            SeriesPosterPath = File.Exists(seriesPoster) ? seriesPoster : null,
            SeriesLogoPath = File.Exists(seriesLogo) ? seriesLogo : null
        };
    }

    private static string? ExtractFromPlot(string? plot, Regex regex)
    {
        if (string.IsNullOrEmpty(plot))
        {
            return null;
        }

        var match = regex.Match(plot);
        if (match.Success)
        {
            var value = match.Groups[1].Value.Trim();
            return string.Equals(value, "Unavailable", StringComparison.OrdinalIgnoreCase) ? null : value;
        }

        return null;
    }

    private static string? CleanPlot(string? plot)
    {
        if (string.IsNullOrEmpty(plot))
        {
            return null;
        }

        var cleaned = MangaChapterRegex.Replace(plot, string.Empty);
        cleaned = AnimeEpisodeRegex.Replace(cleaned, string.Empty);
        cleaned = cleaned.Trim();

        return string.IsNullOrEmpty(cleaned) || string.Equals(cleaned, "Description unavailable.", StringComparison.OrdinalIgnoreCase)
            ? null
            : cleaned;
    }

    /// <inheritdoc/>
    public async Task<ISeries?> FindSeriesAsync(CancellationToken cancellationToken)
    {
        var data = await LoadMetadataAsync().ConfigureAwait(false);
        return data?.Series;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<IArc>> FindAllArcsAsync(CancellationToken cancellationToken)
    {
        var data = await LoadMetadataAsync().ConfigureAwait(false);
        if (data != null)
        {
            return data.Arcs;
        }

        return Array.Empty<IArc>();
    }

    /// <inheritdoc/>
    public async Task<IArc?> FindArcByIdAsync(string id, CancellationToken cancellationToken)
    {
        var data = await LoadMetadataAsync().ConfigureAwait(false);
        return data?.Arcs.FirstOrDefault(a => string.Equals(a.Id, id, StringComparison.Ordinal));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<IEpisode>> FindAllEpisodesAsync(CancellationToken cancellationToken)
    {
        var data = await LoadMetadataAsync().ConfigureAwait(false);
        if (data != null)
        {
            return data.Episodes;
        }

        return Array.Empty<IEpisode>();
    }

    /// <inheritdoc/>
    public async Task<IEpisode?> FindEpisodeByIdAsync(string id, CancellationToken cancellationToken)
    {
        var data = await LoadMetadataAsync().ConfigureAwait(false);
        return data?.Episodes.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.Ordinal));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<IArt>> FindAllLogoArtBySeriesAsync(CancellationToken cancellationToken)
    {
        var data = await LoadMetadataAsync().ConfigureAwait(false);
        if (data?.SeriesLogoPath != null)
        {
            return new IArt[] { new OnePaceArt(data.SeriesLogoPath) };
        }

        return Array.Empty<IArt>();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<IArt>> FindAllCoverArtBySeriesAsync(CancellationToken cancellationToken)
    {
        var data = await LoadMetadataAsync().ConfigureAwait(false);
        if (data?.SeriesPosterPath != null)
        {
            return new IArt[] { new OnePaceArt(data.SeriesPosterPath) };
        }

        return Array.Empty<IArt>();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<IArt>> FindAllCoverArtByArcIdAsync(string arcId, CancellationToken cancellationToken)
    {
        var data = await LoadMetadataAsync().ConfigureAwait(false);
        if (data != null && data.ArcPosterPaths.TryGetValue(arcId, out var posterPath) && posterPath != null)
        {
            return new IArt[] { new OnePaceArt(posterPath) };
        }

        return Array.Empty<IArt>();
    }

    /// <inheritdoc/>
    public Task<IReadOnlyCollection<IArt>> FindAllCoverArtByEpisodeIdAsync(string episodeId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<IArt>>(Array.Empty<IArt>());
    }

    // ---- Domain Model Implementations ----

    private sealed class OnePaceData
    {
        public required OnePaceSeries Series { get; init; }

        public required IReadOnlyList<OnePaceArc> Arcs { get; init; }

        public required IReadOnlyList<OnePaceEpisode> Episodes { get; init; }

        public required Dictionary<string, string?> ArcPosterPaths { get; init; }

        public required string? SeriesPosterPath { get; init; }

        public required string? SeriesLogoPath { get; init; }
    }

    private sealed class OnePaceSeries : ISeries
    {
        public string InvariantTitle => "One Pace";

        public string OriginalTitle => "One Piece";

        public string? Description { get; init; }
    }

    private sealed class OnePaceArc : IArc
    {
        public required string Id { get; init; }

        public required int Rank { get; init; }

        public required string InvariantTitle { get; init; }

        public string? Description { get; init; }

        public string? MangaChapters { get; init; }

        public DateTime? ReleaseDate { get; init; }

        public string? Saga { get; init; }
    }

    private sealed class OnePaceEpisode : IEpisode
    {
        public required string Id { get; init; }

        public required string ArcId { get; init; }

        public required int Rank { get; init; }

        public required string InvariantTitle { get; init; }

        public string? Description { get; init; }

        public string? MangaChapters { get; init; }

        public DateTime? ReleaseDate { get; init; }

        public string? AnimeEpisodes { get; init; }
    }

    private sealed class OnePaceArt : IArt
    {
        public OnePaceArt(string path)
        {
            Url = path;
        }

        public string Url { get; }

        public int? Width => null;

        public int? Height => null;
    }
}
