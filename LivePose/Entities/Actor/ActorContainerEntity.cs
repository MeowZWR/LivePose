using Dalamud.Interface;
using LivePose.Capabilities.Actor;
using LivePose.Entities.Core;
using LivePose.Game.GPose;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace LivePose.Entities.Actor;

public class ActorContainerEntity(IServiceProvider provider) : Entity("actorContainer", provider)
{
    private readonly GPoseService _gPoseService = provider.GetRequiredService<GPoseService>();

    public override string FriendlyName => "Actors";
    public override FontAwesomeIcon Icon => FontAwesomeIcon.Users;

    public override void OnAttached()
    {
        AddCapability(ActivatorUtilities.CreateInstance<ActorContainerCapability>(_serviceProvider, this));
    }

    public override void OnChildAttached() => SortChildren();
    public override void OnChildDetached() => SortChildren();
    
    private void SortChildren()
    {
        _children.Sort((a, b) =>
        {
            if(a is ActorEntity actorA && b is ActorEntity actorB)
                return actorA.GameObject.ObjectIndex.CompareTo(actorB.GameObject.ObjectIndex);

            return string.Compare(a.Id.Unique, b.Id.Unique, System.StringComparison.Ordinal);
        });
    }
}
