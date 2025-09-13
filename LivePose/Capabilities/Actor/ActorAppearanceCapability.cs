using LivePose.Game.Actor.Extensions;
using Dalamud.Plugin.Services;
using LivePose.Entities.Actor;
using LivePose.Game.Actor;
using LivePose.Game.Actor.Appearance;
using LivePose.Game.Core;
using LivePose.Game.GPose;
using LivePose.IPC;
using System;

namespace LivePose.Capabilities.Actor;

public class ActorAppearanceCapability(ActorEntity parent) : ActorCharacterCapability(parent) {
    public ActorAppearance CurrentAppearance => ActorAppearance.FromCharacter(Character);
    public unsafe bool IsHuman => Character.GetHuman() != null;
}
