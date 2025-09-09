using LivePose.Game.Actor.Extensions;
using LivePose.Capabilities.Actor;
using LivePose.Capabilities.Posing;
using LivePose.Core;
using LivePose.Entities.Actor;
using LivePose.Game.Actor.Appearance;
using LivePose.Game.Actor.Interop;
using LivePose.Game.Types;
using MessagePack;
using System;

namespace LivePose.Files;

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class ActorFile
{
    public string Name { get; set; } = "";

    public string FriendlyName { get; set; } = "Actor";

    public required AnamnesisCharaFile AnamnesisCharaFile { get; set; }
    public required PoseFile PoseFile { get; set; }

    public bool HasChild { get; set; }
    public ChildActor? Child { get; set; }
    public PropData? PropData { get; set; }

    public bool HeadSlotShown { get; set; }
    public bool MainHandSlotShown { get; set; }
    public bool OffHandSlotShown { get; set; }

    public bool ActorFrozen { get; set; }
    public bool HasBaseAnimation { get; set; }
    public int BaseAnimation { get; set; }

    public bool IsProp { get; set; }

    public static unsafe implicit operator ActorFile(ActorEntity actorEntity)
    {
        var appearanceCapability = actorEntity.GetCapability<ActorAppearanceCapability>();
        var posingCapability = actorEntity.GetCapability<PosingCapability>();
        var modelCapability = actorEntity.GetCapability<ModelPosingCapability>();

        ActorAppearanceExtended anaCharaFile = new() { Appearance = appearanceCapability.CurrentAppearance };
        BrioHuman.ShaderParams* shaderParams = appearanceCapability.Character.GetShaderParams();

        // append shader params to appearance if they exist
        if(shaderParams is not null)
        {
            anaCharaFile.ShaderParams = *shaderParams;
        }

        var actorFile = new ActorFile
        {
            Name = actorEntity.RawName,
            FriendlyName = actorEntity.FriendlyName,
            AnamnesisCharaFile = anaCharaFile,
            PoseFile = posingCapability.GeneratePoseFile(),
            IsProp = actorEntity.IsProp,
            PropData = new PropData
            {
                //PropID = appearanceCapability.GetProp(),
                PropTransformAbsolute = modelCapability.Transform,
                PropTransformDifference = modelCapability.Transform.CalculateDiff(modelCapability.OriginalTransform)
            }
        };

        return actorFile;
    }
}

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class ChildActor
{
    public required CompanionContainer Companion { get; set; }

    public PoseFile? PoseFile { get; set; }
}

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class PropData
{
    //public FFXIVClientStructs.FFXIV.Client.Game.Character.WeaponModelId PropID { get; set; }
    public Transform PropTransformDifference { get; set; }
    public Transform PropTransformAbsolute { get; set; }
}
