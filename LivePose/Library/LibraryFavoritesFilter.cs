using LivePose.Config;
using LivePose.Library.Filters;

namespace LivePose.Library;

public class LibraryFavoritesFilter : FilterBase
{
    private ConfigurationService _configurationService;

    public LibraryFavoritesFilter(ConfigurationService configurationService)
        : base("Favorites")
    {
        _configurationService = configurationService;
    }

    public override void Clear()
    {
    }

    public override bool Filter(EntryBase entry)
    {
        if(_configurationService.Configuration.Library.Favorites.Contains(entry.Identifier))
            return true;

        return false;
    }
}
