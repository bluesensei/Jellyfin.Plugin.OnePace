using System;

namespace Jellyfin.Plugin.OnePace.Model;

/// <summary>
/// Represents a One Pace arc (mapped to a Jellyfin season).
/// </summary>
public interface IArc
{
    /// <summary>
    /// Gets the unique ID.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the rank (ordering number) of the arc.
    /// </summary>
    int Rank { get; }

    /// <summary>
    /// Gets the invariant title (e.g., "Romance Dawn").
    /// </summary>
    string InvariantTitle { get; }

    /// <summary>
    /// Gets the manga chapters associated with this arc.
    /// </summary>
    string? MangaChapters { get; }

    /// <summary>
    /// Gets the release date.
    /// </summary>
    DateTime? ReleaseDate { get; }

    /// <summary>
    /// Gets the arc description/summary.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the saga this arc belongs to (e.g. "East Blue", "Arabasta").
    /// </summary>
    string? Saga { get; }
}
