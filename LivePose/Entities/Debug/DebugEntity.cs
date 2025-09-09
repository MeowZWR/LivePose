using Dalamud.Interface;
using LivePose.Capabilities.Debug;
using LivePose.Entities.Core;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace LivePose.Entities.Debug;

public class DebugEntity(IServiceProvider provider) : Entity(FixedId, provider)
{
    public const string FixedId = "debug_entity";

    public override string FriendlyName => "Debug";
    public override FontAwesomeIcon Icon => FontAwesomeIcon.Bug;

    public override EntityFlags Flags => base.Flags | EntityFlags.AllowOutSideGpose;

    public override void OnAttached()
    {
        AddCapability(ActivatorUtilities.CreateInstance<DebugCapability>(_serviceProvider, this));
    }
}
