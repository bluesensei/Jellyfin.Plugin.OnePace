using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.OnePace;

internal static class ProviderIdsExtensions
{
    public static void SetOnePaceId(this IHasProviderIds hasProviderIds, string id)
    {
        hasProviderIds.SetProviderId(Plugin.ProviderName, id);
    }

    public static string? GetOnePaceId(this IHasProviderIds hasProviderIds)
    {
        var id = hasProviderIds.GetProviderId(Plugin.ProviderName);
        return !string.IsNullOrEmpty(id) ? id : null;
    }
}
