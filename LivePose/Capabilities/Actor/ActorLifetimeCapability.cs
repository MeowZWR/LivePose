using LivePose.Game.Actor.Extensions;
using Dalamud.Game.ClientState.Objects.Types;
using LivePose.Entities;
using LivePose.Entities.Actor;
using LivePose.Game.Actor;
using LivePose.Game.Core;
using LivePose.UI.Widgets.Actor;

namespace LivePose.Capabilities.Actor;

public class ActorLifetimeCapability : ActorCapability
{
    private readonly TargetService _targetService;
    
    private readonly EntityManager _entityManager;
    public ActorLifetimeCapability(ActorEntity parent, TargetService targetService, EntityManager entityManager) : base(parent)
    {
        _targetService = targetService;
        _entityManager = entityManager;

        Widget = new ActorLifetimeWidget(this);
    }

    public void Target()
    {
        _targetService.GPoseTarget = GameObject;
    }

    public bool CanClone => Actor.Parent is ActorContainerEntity && GameObject is ICharacter;
    

    public bool CanDestroy =>
        Actor.Parent is ActorContainerEntity ||
        (Actor.Parent is ActorEntity parentEntity && parentEntity.GameObject is ICharacter character && character.HasSpawnedCompanion());

}
