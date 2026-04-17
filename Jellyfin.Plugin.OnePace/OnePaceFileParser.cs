using System;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.OnePace;

/// <summary>
/// Parses One Pace filenames and folder names to extract metadata.
/// Handles formats like:
///   [One Pace][237-238] Skypiea 01 [720p][En Dub][5B9B150E].mp4
///   [One Pace][1-7] Romance Dawn [1080p]
///   [One Pace][3-5] Romance Dawn 03 [1080p][C7CA5080].mkv
/// </summary>
internal static class OnePaceFileParser
{
    // Matches a full One Pace episode filename
    // Groups: 1=chapter range, 2=arc name, 3=episode number, 4=resolution, 5=extra tags, 6=CRC32
    private static readonly Regex EpisodeFileRegex = new(
        @"\[One\s*Pace\]\s*\[([^\]]+)\]\s*(.+?)\s+(\d+)\s*(?:\[([^\]]*)\])?\s*(?:\[([^\]]*)\])?\s*(?:\[([0-9A-Fa-f]{8})\])?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Matches an arc/season folder name
    // Groups: 1=chapter range, 2=arc name, 3=resolution (optional)
    private static readonly Regex ArcFolderRegex = new(
        @"\[One\s*Pace\]\s*\[([^\]]+)\]\s*(.+?)(?:\s*\[([^\]]*)\])?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Fallback: just find a CRC32 in square brackets
    private static readonly Regex Crc32Regex = new(
        @"\[([0-9A-Fa-f]{8})\]",
        RegexOptions.Compiled);

    // Extract episode number from any string like "Skypiea 01" or "Romance Dawn 03"
    private static readonly Regex TrailingNumberRegex = new(
        @"(\d+)\s*$",
        RegexOptions.Compiled);

    /// <summary>
    /// Result of parsing a One Pace episode filename.
    /// </summary>
    public sealed class EpisodeParseResult
    {
        /// <summary>Gets the manga chapter range string (e.g. "237-238").</summary>
        public string? ChapterRange { get; init; }

        /// <summary>Gets the arc name (e.g. "Skypiea").</summary>
        public string? ArcName { get; init; }

        /// <summary>Gets the episode number within the arc.</summary>
        public int? EpisodeNumber { get; init; }

        /// <summary>Gets the CRC-32 checksum as a hex string.</summary>
        public string? Crc32Hex { get; init; }

        /// <summary>Gets the CRC-32 as a uint value.</summary>
        public uint? Crc32Value { get; init; }

        /// <summary>Gets the resolution string (e.g. "720p").</summary>
        public string? Resolution { get; init; }
    }

    /// <summary>
    /// Result of parsing a One Pace arc folder name.
    /// </summary>
    public sealed class ArcParseResult
    {
        /// <summary>Gets the manga chapter range string.</summary>
        public string? ChapterRange { get; init; }

        /// <summary>Gets the arc name.</summary>
        public string? ArcName { get; init; }

        /// <summary>Gets the resolution string.</summary>
        public string? Resolution { get; init; }
    }

    /// <summary>
    /// Checks if the given path or name looks like a One Pace file/folder.
    /// </summary>
    public static bool IsOnePace(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return false;
        }

        return IdentifierUtil.OnePaceInvariantTitleRegex.IsMatch(input);
    }

    /// <summary>
    /// Parses a One Pace episode filename.
    /// </summary>
    public static EpisodeParseResult? ParseEpisodeFileName(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return null;
        }

        var match = EpisodeFileRegex.Match(fileName);
        if (match.Success)
        {
            string? crc32Hex = match.Groups[6].Success ? match.Groups[6].Value : null;
            uint? crc32Value = null;
            if (crc32Hex != null)
            {
                crc32Value = Convert.ToUInt32(crc32Hex, 16);
            }

            int? episodeNumber = null;
            if (int.TryParse(match.Groups[3].Value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int epNum))
            {
                episodeNumber = epNum;
            }

            return new EpisodeParseResult
            {
                ChapterRange = match.Groups[1].Value.Trim(),
                ArcName = match.Groups[2].Value.Trim(),
                EpisodeNumber = episodeNumber,
                Resolution = match.Groups[4].Success ? match.Groups[4].Value.Trim() : null,
                Crc32Hex = crc32Hex,
                Crc32Value = crc32Value
            };
        }

        // Fallback: try to find CRC32 and episode number separately
        if (IsOnePace(fileName))
        {
            string? crc32Hex = null;
            uint? crc32Value = null;
            var crcMatch = Crc32Regex.Match(fileName);
            if (crcMatch.Success)
            {
                crc32Hex = crcMatch.Groups[1].Value;
                crc32Value = Convert.ToUInt32(crc32Hex, 16);
            }

            int? episodeNumber = null;
            var numMatch = TrailingNumberRegex.Match(
                Regex.Replace(fileName, @"\[.*?\]", " ").Trim());
            if (numMatch.Success && int.TryParse(numMatch.Groups[1].Value, out int num))
            {
                episodeNumber = num;
            }

            return new EpisodeParseResult
            {
                ChapterRange = ExtractChapterRange(fileName),
                ArcName = ExtractArcName(fileName),
                EpisodeNumber = episodeNumber,
                Crc32Hex = crc32Hex,
                Crc32Value = crc32Value
            };
        }

        return null;
    }

    /// <summary>
    /// Parses a One Pace arc/season folder name.
    /// </summary>
    public static ArcParseResult? ParseArcFolderName(string? folderName)
    {
        if (string.IsNullOrEmpty(folderName))
        {
            return null;
        }

        var match = ArcFolderRegex.Match(folderName);
        if (match.Success)
        {
            return new ArcParseResult
            {
                ChapterRange = match.Groups[1].Value.Trim(),
                ArcName = match.Groups[2].Value.Trim(),
                Resolution = match.Groups[3].Success ? match.Groups[3].Value.Trim() : null
            };
        }

        // Fallback: try to extract arc name
        if (IsOnePace(folderName))
        {
            return new ArcParseResult
            {
                ChapterRange = ExtractChapterRange(folderName),
                ArcName = ExtractArcName(folderName)
            };
        }

        return null;
    }

    private static string? ExtractChapterRange(string input)
    {
        var match = Regex.Match(input, @"\[One\s*Pace\]\s*\[([^\]]+)\]", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private static string? ExtractArcName(string input)
    {
        // Remove all bracket groups and trim
        var cleaned = Regex.Replace(input, @"\[.*?\]", " ").Trim();
        // Remove trailing episode number
        cleaned = Regex.Replace(cleaned, @"\s+\d+\s*$", "").Trim();
        // Remove file extension
        cleaned = Regex.Replace(cleaned, @"\.\w{2,4}$", "").Trim();
        return string.IsNullOrEmpty(cleaned) ? null : cleaned;
    }
}
