using LivePose.Capabilities.Posing;
using LivePose.Config;
using LivePose.Entities.Actor;
using LivePose.Game.Actor;
using LivePose.UI.Widgets.Actor;
using System.Collections.Generic;

namespace LivePose.Capabilities.Actor;

public class ActorDebugCapability : ActorCharacterCapability
{

    public bool IsDebug => _configService.IsDebug;

    private readonly ConfigurationService _configService;

    public ActorDebugCapability(ActorEntity parent, ConfigurationService configService) : base(parent)
    {
        _configService = configService;

        Widget = new ActorDebugWidget(this);
    }

    public Dictionary<string, int> SkeletonStacks
    {
        get
        {
            if(Entity.TryGetCapability<SkeletonPosingCapability>(out var capability))
                return capability.PoseInfo.StackCounts;

            return [];
        }
    }
}
