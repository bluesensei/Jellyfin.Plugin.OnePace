namespace Jellyfin.Plugin.OnePace.Model;

/// <summary>
/// Represents the One Pace series.
/// </summary>
public interface ISeries
{
    /// <summary>
    /// Gets the invariant title ("One Pace").
    /// </summary>
    string InvariantTitle { get; }

    /// <summary>
    /// Gets the original title ("One Piece").
    /// </summary>
    string OriginalTitle { get; }

    /// <summary>
    /// Gets the series description/summary.
    /// </summary>
    string? Description { get; }
}
