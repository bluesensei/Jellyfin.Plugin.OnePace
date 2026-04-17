using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.OnePace.Model;

namespace Jellyfin.Plugin.OnePace;

/// <summary>
/// Provides One Pace metadata.
/// </summary>
public interface IRepository
{
    /// <summary>
    /// Gets the series metadata.
    /// </summary>
    Task<ISeries?> FindSeriesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets all known arcs.
    /// </summary>
    Task<IReadOnlyCollection<IArc>> FindAllArcsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets an arc by ID.
    /// </summary>
    Task<IArc?> FindArcByIdAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all known episodes.
    /// </summary>
    Task<IReadOnlyCollection<IEpisode>> FindAllEpisodesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets an episode by ID.
    /// </summary>
    Task<IEpisode?> FindEpisodeByIdAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    /// Gets series logo art.
    /// </summary>
    Task<IReadOnlyCollection<IArt>> FindAllLogoArtBySeriesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets series cover art.
    /// </summary>
    Task<IReadOnlyCollection<IArt>> FindAllCoverArtBySeriesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets arc cover art.
    /// </summary>
    Task<IReadOnlyCollection<IArt>> FindAllCoverArtByArcIdAsync(string arcId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets episode cover art.
    /// </summary>
    Task<IReadOnlyCollection<IArt>> FindAllCoverArtByEpisodeIdAsync(string episodeId, CancellationToken cancellationToken);
}
