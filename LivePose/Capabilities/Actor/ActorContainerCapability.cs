using Dalamud.Game.ClientState.Objects.Types;
using LivePose.Capabilities.Core;
using LivePose.Entities;
using LivePose.Entities.Actor;
using LivePose.Entities.Core;
using LivePose.Game.Actor;
using LivePose.Game.Core;
using LivePose.Game.GPose;
using LivePose.UI.Widgets.Actor;
using System;

namespace LivePose.Capabilities.Actor;

public class ActorContainerCapability : Capability
{
    private readonly EntityManager _entityManager;
    private readonly TargetService _targetService;
    private readonly GPoseService _gPoseService;

    public bool CanControlCharacters => _gPoseService.IsGPosing;

    public ActorContainerCapability(ActorContainerEntity parent, EntityManager entityManager, TargetService targetService, GPoseService gPoseService) : base(parent)
    {
        _entityManager = entityManager;
        _targetService = targetService;
        _gPoseService = gPoseService;
        Widget = new ActorContainerWidget(this);
    }

    public void SelectActorInHierarchy(ActorEntity entity)
    {
        _entityManager.SetSelectedEntity(entity);
    }

    public void Target(ActorEntity entity)
    {
        _targetService.GPoseTarget = entity.GameObject;
    }

    public void SelectInHierarchy(ActorEntity entity)
    {
        _entityManager.SetSelectedEntity(entity);
    }
}
