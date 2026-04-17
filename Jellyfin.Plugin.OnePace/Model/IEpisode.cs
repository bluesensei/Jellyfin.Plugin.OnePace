using System;

namespace Jellyfin.Plugin.OnePace.Model;

/// <summary>
/// Represents a One Pace episode within an arc.
/// </summary>
public interface IEpisode
{
    /// <summary>
    /// Gets the unique ID.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the arc ID this episode belongs to.
    /// </summary>
    string ArcId { get; }

    /// <summary>
    /// Gets the rank (episode number) within the arc.
    /// </summary>
    int Rank { get; }

    /// <summary>
    /// Gets the invariant title (e.g., "Romance Dawn 01").
    /// </summary>
    string InvariantTitle { get; }

    /// <summary>
    /// Gets the manga chapters for this episode.
    /// </summary>
    string? MangaChapters { get; }

    /// <summary>
    /// Gets the release date.
    /// </summary>
    DateTime? ReleaseDate { get; }

    /// <summary>
    /// Gets the episode description/summary.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the anime episode(s) covered.
    /// </summary>
    string? AnimeEpisodes { get; }
}
