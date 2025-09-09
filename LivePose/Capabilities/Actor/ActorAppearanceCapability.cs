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

public class ActorAppearanceCapability : ActorCharacterCapability
{
    private readonly TargetService _targetService;

    private readonly GPoseService _gposeService;
    private readonly IFramework _framework;

    public bool IsCollectionOverridden => _oldCollection != null;
    private string? _oldCollection = null;

    public bool IsDesignOverridden;
    public bool IsProfileOverridden;

    public string CurrentDesign { get; set; } = "None";


    public (string? name, Guid? id) SelectedDesign { get; set; } = ("None", null);


    private ActorAppearance? _originalAppearance = null;
    public bool IsAppearanceOverridden => _originalAppearance.HasValue || IsDesignOverridden || IsProfileOverridden | IsCollectionOverridden;

    public ActorAppearance CurrentAppearance => ActorAppearance.FromCharacter(Character);

    public ActorAppearance OriginalAppearance => _originalAppearance ?? CurrentAppearance;

    public ModelShaderOverride _modelShaderOverride = new();

    public unsafe bool IsHuman => Character.GetHuman() != null;
    
    public bool IsSelf => _targetService.IsSelf(GameObject);

    public bool IsHidden => CurrentAppearance.ExtendedAppearance.Transparency == 0;

    public ActorAppearanceCapability(ActorEntity parent, IFramework framework, TargetService targetService, GPoseService gPoseService) : base(parent)
    {
        _gposeService = gPoseService;
        _framework = framework;
        _targetService = targetService;
    }
    

    public override void Dispose()
    {

    }
}
