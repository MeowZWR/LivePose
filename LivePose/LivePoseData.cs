using System.Collections.Generic;
using System.Numerics;
using LivePose.Core;
using LivePose.Game.Posing;
using Newtonsoft.Json;
using OneOf;

namespace LivePose;

public class BonePoseData {
    public Transform Transform;
    public TransformComponents Propogate;
    
    public bool IK_Enabled;
    public bool IK_EnforceConstraints;
    
    public int IK_Type;
    
    public int IK_Arg0;
    public int IK_Arg1;
    
    public int IK_Arg2;
    public Vector3 IK_RotationAxis;
    

    public bool ShouldSerializeIK_Enabled() => IK_Enabled;
    public bool ShouldSerializeIK_EnforceConstraints() => IK_Enabled && IK_EnforceConstraints;
    public bool ShouldSerializeIK_Type() => IK_Enabled;
    
    public bool ShouldSerializeIK_Arg0() => IK_Enabled;
    public bool ShouldSerializeIK_Arg1() => IK_Enabled;
    public bool ShouldSerializeIK_Arg2() => IK_Enabled && IK_Type == 1;
    public bool ShouldSerializeIK_RotationAxis() => IK_Enabled && IK_Type == 1;
    
    
    [JsonIgnore] private BoneIKInfo? boneIkInfo;

    [JsonIgnore]
    public BoneIKInfo BoneIkInfo {
        get {
            if(boneIkInfo != null) return boneIkInfo.Value;

            if(!IK_Enabled) return (this.boneIkInfo = BoneIKInfo.Disabled).Value;
            
            boneIkInfo = new BoneIKInfo() {
                Enabled = true,
                EnforceConstraints = IK_EnforceConstraints,
                SolverOptions = IK_Type switch {
                    1 => OneOf<BoneIKInfo.CCDOptions, BoneIKInfo.TwoJointOptions>.FromT1(new BoneIKInfo.TwoJointOptions() { FirstBone = IK_Arg0, SecondBone = IK_Arg1, EndBone = IK_Arg2, RotationAxis = IK_RotationAxis }),
                    _ => OneOf<BoneIKInfo.CCDOptions, BoneIKInfo.TwoJointOptions>.FromT0(new BoneIKInfo.CCDOptions() { Depth = IK_Arg0, Iterations = IK_Arg1 })
                }
            };
            
            return boneIkInfo.Value;

        }
    }
}

public class LivePoseData {
    public Dictionary<string, List<BonePoseData>> Pose = [];
    
    
    public bool Frozen = false;

    public string Serialize() {
        return JsonConvert.SerializeObject(this);
    }

    public static LivePoseData? Deserialize(string json) {
        try {
            return JsonConvert.DeserializeObject<LivePoseData>(json);
        } catch {
            return null;
        }
    }
    
}
