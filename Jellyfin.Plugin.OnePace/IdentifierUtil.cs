using System;
using System.Linq;
using System.Text.RegularExpressions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.OnePace;

internal static class IdentifierUtil
{
    public static readonly Regex OnePaceInvariantTitleRegex = BuildTextRegex("One Pace");

    public static Regex BuildTextRegex(string needle)
    {
        var pattern = @"\b" + string.Join(@"\s+", needle.Split().Select(Regex.Escape)) + @"\b";

        // Handle the common "Whisky" vs "Whiskey" typo in One Pace files
        pattern = pattern.Replace("Whisky", "Whiske?y", StringComparison.InvariantCultureIgnoreCase);

        return new Regex(pattern, RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Checks whether a path is under the user-configured One Pace library path.
    /// </summary>
    public static bool IsUnderLibraryPath(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        var libraryPath = Plugin.Instance?.Configuration?.LibraryPath;
        if (string.IsNullOrEmpty(libraryPath))
        {
            return false;
        }

        return path.StartsWith(libraryPath, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Copies provider IDs from a BaseItem to an ItemLookupInfo so identifiers can be reused.
    /// </summary>
    internal static void CopyProviderIds(BaseItem source, ItemLookupInfo target)
    {
        if (source.ProviderIds != null)
        {
            foreach (var kvp in source.ProviderIds)
            {
                target.ProviderIds[kvp.Key] = kvp.Value;
            }
        }
    }
}
