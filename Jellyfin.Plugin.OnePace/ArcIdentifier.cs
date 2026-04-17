using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.OnePace.Model;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.OnePace;

internal static class ArcIdentifier
{
    public static async Task<IArc?> IdentifyAsync(
        IRepository repository,
        ItemLookupInfo itemLookupInfo,
        CancellationToken cancellationToken)
    {
        // First, try matching by stored provider ID
        var arcId = itemLookupInfo.GetOnePaceId();
        if (arcId != null)
        {
            var arc = await repository.FindArcByIdAsync(arcId, cancellationToken).ConfigureAwait(false);
            if (arc != null)
            {
                return arc;
            }
        }

        if (itemLookupInfo.Path == null ||
            (!IdentifierUtil.OnePaceInvariantTitleRegex.IsMatch(itemLookupInfo.Path)
             && !IdentifierUtil.IsUnderLibraryPath(itemLookupInfo.Path)))
        {
            return null;
        }

        var arcs = await repository.FindAllArcsAsync(cancellationToken).ConfigureAwait(false);
        var directoryName = Path.GetFileName(itemLookupInfo.Path);

        // Try parsing with the One Pace filename parser
        var parsed = OnePaceFileParser.ParseArcFolderName(directoryName);
        if (parsed?.ArcName != null)
        {
            foreach (var arc in arcs.OrderByDescending(a => a.InvariantTitle.Length))
            {
                if (IdentifierUtil.BuildTextRegex(arc.InvariantTitle).IsMatch(parsed.ArcName))
                {
                    return arc;
                }
            }
        }

        // Match against arc invariant titles directly (handles plain folder names like "Skypiea")
        foreach (var arc in arcs.OrderByDescending(a => a.InvariantTitle.Length))
        {
            if (IdentifierUtil.BuildTextRegex(arc.InvariantTitle).IsMatch(directoryName))
            {
                return arc;
            }
        }

        return null;
    }
}
