using LivePose.Config;
using LivePose.Entities;
using LivePose.Entities.Actor;
using LivePose.Library.Sources;
using LivePose.UI.Controls.Stateless;

namespace LivePose.Files;

public abstract class AppliableActorFileInfoBase<T> : JsonDocumentBaseFileInfo<T>
    where T : class
{
    private EntityManager _entityManager;
    private ConfigurationService _configService;

    public AppliableActorFileInfoBase(EntityManager entityManager, ConfigurationService configurationService)
    {
        _entityManager = entityManager;
        _configService = configurationService;
    }

    public override bool InvokeDefaultAction(FileEntry fileEntry, object? args)
    {
        if(args is not null and ActorEntity actor)
        {
            if(Load(fileEntry.FilePath) is T file)
            {
                Apply(file, actor, false);
                return true;
            }
        }

        return false;
    }

    protected abstract void Apply(T file, ActorEntity actor, bool asExpression);
}
