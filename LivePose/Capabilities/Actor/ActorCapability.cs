using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using LivePose.Capabilities.Core;
using LivePose.Config;
using LivePose.Core;
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
    
    
    public unsafe Character*  NativeCharacter => (Character*)Character.Address;

    public CharacterConfiguration CharacterConfiguration => Actor.CharacterConfiguration;
    
    
    public unsafe bool IsReady {
        get {
            var chr = NativeCharacter;
            if(chr == null) return false;
            if(chr->DrawObject == null) return false;
            if(!chr->DrawObject->IsVisible) return false;
            if(chr->DrawObject->GetObjectType() != ObjectType.CharacterBase) return false;
            var nativeCharacterBase = (CharacterBase*)chr->DrawObject;
            if(nativeCharacterBase->GetModelType() != CharacterBase.ModelType.Human) return false;
            if(LivePose.TryGetService(out ICondition condition) && condition.AnyUnsafe()) return false;
            return true;
        }
    }
}
