using Dalamud.Interface;
using LivePose.Capabilities.Actor;
using LivePose.Entities.Core;
using LivePose.Game.GPose;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace LivePose.Entities.Actor;

public class ActorContainerEntity(IServiceProvider provider) : Entity("actorContainer", provider)
{
    public override string FriendlyName => "Actors";
    public override FontAwesomeIcon Icon => FontAwesomeIcon.Users;
}
