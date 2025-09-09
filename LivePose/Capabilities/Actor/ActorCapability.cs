using Dalamud.Game.ClientState.Objects.Types;
using LivePose.Capabilities.Core;
using LivePose.Entities.Actor;

namespace LivePose.Capabilities.Actor;

public abstract class ActorCapability(ActorEntity parent) : Capability(parent)
{
    public ActorEntity Actor => (ActorEntity)Entity;

    public IGameObject GameObject => Actor.GameObject;
}

public abstract class ActorCharacterCapability(ActorEntity parent) : ActorCapability(parent)
{
    public ICharacter Character => (ICharacter)GameObject;
}
