using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.OnePace.Model;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.OnePace;

internal static class SeriesIdentifier
{
    public static async Task<ISeries?> IdentifyAsync(
        IRepository repository,
        ItemLookupInfo itemLookupInfo,
        CancellationToken cancellationToken)
    {
        if (itemLookupInfo.GetOnePaceId() == Plugin.DummySeriesId
            || (itemLookupInfo.Name != null && IdentifierUtil.OnePaceInvariantTitleRegex.IsMatch(itemLookupInfo.Name))
            || (itemLookupInfo.Path != null && IdentifierUtil.OnePaceInvariantTitleRegex.IsMatch(itemLookupInfo.Path))
            || IdentifierUtil.IsUnderLibraryPath(itemLookupInfo.Path))
        {
            return await repository.FindSeriesAsync(cancellationToken).ConfigureAwait(false);
        }

        return null;
    }
}
