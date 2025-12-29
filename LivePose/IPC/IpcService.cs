using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using LivePose.Capabilities.Actor;
using LivePose.Capabilities.Posing;
using LivePose.Config;
using LivePose.Entities;
using LivePose.Entities.Core;
using LivePose.Game.Posing;
using System;
using System.Collections.Generic;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using LivePose.Core;
using LivePose.Entities.Actor;
using LivePose.Game.Actor;

namespace LivePose.IPC;
public class IpcService : IDisposable
{
    public static readonly (int, int) CurrentApiVersion = (1, 0);

    public bool IsIPCEnabled { get; private set; } = false;

    //

    public const string ApiVersion_IPCName = "LivePose.ApiVersion";
    private ICallGateProvider<(int, int)>? API_Version_IPC;
    
    public const string GetPose_IPCName = "LivePose.GetPose";
    private ICallGateProvider<ushort, string>? GetPose_IPC;
    
    public const string SetPose_IPCName = "LivePose.SetPose";
    private ICallGateProvider<ushort, string, bool>? SetPose_IPC;
    
    
    //
    
    private readonly ConfigurationService _configurationService;
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly EntityManager _entityManager;
    private readonly EntityActorManager _entityActorManager;
    private readonly IObjectTable _objectTable;
    private readonly IPluginLog _pluginLog;
    private readonly FakePoseService _fakePoseService;
    private readonly IFramework _framework;
    
    public IpcService(ConfigurationService configurationService, IDalamudPluginInterface pluginInterface, IObjectTable objectTable, IFramework framework, EntityManager entityManager, EntityActorManager entityActorManager,  IPluginLog pluginLog, FakePoseService fakePoseService)
    {
        _configurationService = configurationService;
        _pluginInterface = pluginInterface;
        _entityManager = entityManager;
        _entityActorManager = entityActorManager;
        _objectTable = objectTable;
        _pluginLog = pluginLog;
        _fakePoseService = fakePoseService;
        _framework = framework;
        

        CreateIPC();
    }
    private void CreateIPC()
    {
        API_Version_IPC = _pluginInterface.GetIpcProvider<(int, int)>(ApiVersion_IPCName);
        API_Version_IPC.RegisterFunc(ApiVersion_Impl);

        GetPose_IPC = _pluginInterface.GetIpcProvider<ushort, string>(GetPose_IPCName);
        GetPose_IPC.RegisterFunc(GetPose);
        
        SetPose_IPC = _pluginInterface.GetIpcProvider<ushort, string, bool>(SetPose_IPCName);
        SetPose_IPC.RegisterAction(SetPose);
        

        IsIPCEnabled = true;
    }
    private void DisposeIPC()
    {
        API_Version_IPC?.UnregisterFunc();
        API_Version_IPC = null;
        
        IsIPCEnabled = false;
    }

    //


    private const int RoundValues = 5;

    private (int, int) ApiVersion_Impl() => CurrentApiVersion;


    public PoseInfo DeserializePose(LivePoseData livePoseData) {

        var pose = new PoseInfo();
        
        
        foreach(var boneEntry in livePoseData) {
            var bone = pose.GetPoseInfo(boneEntry.BonePoseInfoId);
            foreach(var s in boneEntry.Stacks) {
                bone.Apply(s.Transform, null, s.Propogate, TransformComponents.All, s.BoneIkInfo);
            }
        }


        return pose;
    }
    
    public LivePoseData SerializePose(SkeletonPosingCapability skeletonPosingCapability, PoseInfo pose) {
        var boneList = new LivePoseData();
        foreach(var b in pose.StackCounts.Keys) {
            var bone = skeletonPosingCapability.GetBone(b);
            if (bone == null) continue;

            var bonePose = pose.GetPoseInfo(bone, b.Slot);
            if (!bonePose.HasStacks) continue;
            
            var list = new List<BonePoseData>();
            foreach(var p in bonePose.Stacks) {
                if (p.Transform.IsApproximatelySame(Transform.Identity)) continue;
                
                var bonePoseData = new BonePoseData {
                    Transform = p.Transform,
                    Propogate = p.PropagateComponents,
                    
                    IK_Enabled = p.IKInfo.Enabled,
                    IK_Type = p.IKInfo.SolverOptions.Index,
                    IK_Arg0 = p.IKInfo.SolverOptions.Index switch {
                        0 => p.IKInfo.SolverOptions.AsT0.Depth,
                        1 => p.IKInfo.SolverOptions.AsT1.FirstBone,
                        _ => throw new ArgumentOutOfRangeException()
                    },
                    IK_Arg1 = p.IKInfo.SolverOptions.Index switch {
                        0 => p.IKInfo.SolverOptions.AsT0.Iterations,
                        1 => p.IKInfo.SolverOptions.AsT1.SecondBone,
                        _ => throw new ArgumentOutOfRangeException()
                    },
                    IK_Arg2 = p.IKInfo.SolverOptions.Index switch {
                        0 => 0,
                        1 => p.IKInfo.SolverOptions.AsT1.EndBone,
                        _ => throw new ArgumentOutOfRangeException()
                    },
                    IK_RotationAxis = p.IKInfo.SolverOptions.Index switch {
                        0 => Vector3.Zero,
                        1 => p.IKInfo.SolverOptions.AsT1.RotationAxis,
                        _ => throw new ArgumentOutOfRangeException()
                    }
                };

                list.Add(bonePoseData);
            }

            boneList.Add(new LivePoseBoneEntry(bonePose.Id, list));
        }

        return boneList;
    }
    
    public string GetPose(ushort objectIndex) {
        if(objectIndex > 200) return string.Empty;
        var obj = _objectTable[objectIndex];
        if(obj == null || !_entityManager.TryGetEntity(new EntityId(obj), out var entity) || entity is not ActorEntity actorEntity) return string.Empty;
        if (!actorEntity.TryGetCapability<ActionTimelineCapability>(out var timelineCapability)) return string.Empty;
        if (!actorEntity.TryGetCapability<SkeletonPosingCapability>(out var skeletonPosingCapability)) return string.Empty;

        if(objectIndex == 0) {
            skeletonPosingCapability.UpdatePoseCache();
        }

        var data = new LivePoseCharacterData();
        if(_configurationService.Configuration.Posing.CursedMode) {
            data.CursedPose = SerializePose(skeletonPosingCapability, skeletonPosingCapability.PoseInfo);
        } else {
            foreach(var (key, p) in skeletonPosingCapability.BodyPoses) {
                var pose = SerializePose(skeletonPosingCapability, p);
                if(pose.Count > 0) {
                    data.BodyPoses.Add(new LivePoseCacheEntry(key.Item1, key.Item2, pose));
                }
            }
        
            foreach(var (key, p) in skeletonPosingCapability.FacePoses) {
                var pose = SerializePose(skeletonPosingCapability, p);
                if(pose.Count > 0)
                    data.FacePoses.Add(new LivePoseCacheEntry(key, pose));
            }

            foreach(var (id, p) in skeletonPosingCapability.MinionPoses) {
                var pose = SerializePose(skeletonPosingCapability, p);
                if(pose.Count > 0) {
                    data.MinionPoses.Add(new LivePoseMinionEntry(id, pose));
                }
            }
        }

        data.AnimationSpeedMultiplier = timelineCapability.SpeedMultiplierOverride;
        data.MinionAnimationSpeedMultiplier = timelineCapability.MinionSpeedMultiplierOverride;
        data.Frozen = timelineCapability.SpeedMultiplierOverride == 0;
        data.MinionLock = skeletonPosingCapability.MinionLock;

        if(data.Frozen) {
            unsafe {
                data.AnimationState = GetAnimationState(timelineCapability.NativeCharacter);
            }
        }
        
        return data.Serialize();
    }


    public void SetPose(ushort objectIndex, string data) {
        if(objectIndex > 200) return;
        var obj = _objectTable[objectIndex];
        if(obj == null) {
            _pluginLog.Warning($"Attempted to set Object#{objectIndex} pose. Object does not exist.");
            return;
        }
        
        _fakePoseService.SetPosed(obj, true);
        _entityActorManager.AttachActor(obj);
        _fakePoseService.SetPosed(obj, false);
        
        
        if(!_entityManager.TryGetEntity(new EntityId(obj), out var entity) || entity is not ActorEntity actorEntity) {
            _pluginLog.Warning($"Attempted to set Object#{objectIndex} pose. Entity invalid.");
            return;
        };

        if(!actorEntity.TryGetCapability<SkeletonPosingCapability>(out var skeletonPosingCapability)) {
            _pluginLog.Warning($"Attempted to set Object#{objectIndex} pose. No SkeletonPosingCapability found.");
            return;
        }

        if(!actorEntity.TryGetCapability<ActionTimelineCapability>(out var timelineCapability)) {
            _pluginLog.Warning($"Attempted to set Object#{objectIndex} pose. No ActionTimelineCapability found.");
            return;
        }

        skeletonPosingCapability.IpcDataJson = data;
        var livePoseData = LivePoseCharacterData.Deserialize(data);
        
        _framework.RunOnTick(() => {
            skeletonPosingCapability.ResetPose();
            if(livePoseData == null) {
                return;
            }
            
            LivePose.Log.Verbose($"Applying Pose to GameObject#{obj.ObjectIndex} => {data}");
            skeletonPosingCapability.CursedMode = livePoseData.CursedPose != null;
            if(livePoseData.CursedPose != null) {
                skeletonPosingCapability.PoseInfo = DeserializePose(livePoseData.CursedPose);
            } else {
                skeletonPosingCapability.BodyPoses.Clear();
                skeletonPosingCapability.FacePoses.Clear();
                skeletonPosingCapability.MinionPoses.Clear();
                foreach(var pose in livePoseData.BodyPoses) {
                    skeletonPosingCapability.BodyPoses[(pose.TimelineId, pose.SecondaryTimelineId)] = DeserializePose(pose.Pose);
                }
            
                foreach(var pose in livePoseData.FacePoses) {
                    skeletonPosingCapability.FacePoses[pose.TimelineId] = DeserializePose(pose.Pose);
                }

                foreach(var pose in livePoseData.MinionPoses) {
                    skeletonPosingCapability.MinionPoses[pose.Minion] = DeserializePose(pose.Pose);
                }
                
                skeletonPosingCapability.ApplyTimelinePose();
                skeletonPosingCapability.ApplyMinionPose();
            }
            
            if(livePoseData.Frozen) {
                timelineCapability.SetOverallSpeedOverride(0f);
                unsafe {
                    SetAnimationState(timelineCapability.NativeCharacter, livePoseData.AnimationState);
                }
            } else {
                if(livePoseData.AnimationSpeedMultiplier.HasValue && !livePoseData.AnimationSpeedMultiplier.Value.IsApproximatelySame(1)) {
                    timelineCapability.SetOverallSpeedOverride(livePoseData.AnimationSpeedMultiplier.Value);
                } else {
                    timelineCapability.ResetOverallSpeedOverride();
                }
            }

            if(livePoseData.MinionLock != null) {
                skeletonPosingCapability.LockMinionPosition(livePoseData.MinionLock);
            } else {
                skeletonPosingCapability.UnlockMinionPosition();
            }
            
            if(livePoseData.MinionAnimationSpeedMultiplier != null && !livePoseData.MinionAnimationSpeedMultiplier.Value.IsApproximatelySame(1)) {
                timelineCapability.SetMinionSpeedOverride(livePoseData.MinionAnimationSpeedMultiplier.Value);
            } else {
                timelineCapability.ResetMinionSpeedOverride();
            }
        }, delayTicks: 1);
    }



    public void Dispose()
    {
        DisposeIPC();
    }

    public unsafe List<AnimationState> GetAnimationState(Character* character) {
        var state = new List<AnimationState>();
        if(character == null) return state;
        if(!character->IsCharacter()) return state;
        if(character->DrawObject == null) return state;
        if (character->DrawObject->GetObjectType() != ObjectType.CharacterBase) return state;
        if (((CharacterBase*)character->DrawObject)->GetModelType() != CharacterBase.ModelType.Human) return state;
        var human = (Human*)character->DrawObject;
        var skeleton = human->Skeleton;
        if (skeleton == null) return state;
        for (var i = 0; i < skeleton->PartialSkeletonCount && i < 1; ++i) {
            var partialSkeleton = &skeleton->PartialSkeletons[i];
            var animatedSkeleton = partialSkeleton->GetHavokAnimatedSkeleton(0);
            if (animatedSkeleton == null) continue;
            for (var animControl = 0; animControl < animatedSkeleton->AnimationControls.Length && animControl < 1; ++animControl) {
                var control = animatedSkeleton->AnimationControls[animControl].Value;
                if (control == null) continue;
                state.Add(new AnimationState(i, animControl, control->hkaAnimationControl.LocalTime));
            }
        }
        
        return state;
    }
    
    private unsafe void SetAnimationState(Character* character, List<AnimationState> state) {
        if(character == null) return;
        if(state.Count == 0) return;
        _framework.RunOnTick(() => {
            if(!character->IsCharacter()) return;
            if(character->DrawObject == null) return;
            if (character->DrawObject->GetObjectType() != ObjectType.CharacterBase) return;
            if (((CharacterBase*)character->DrawObject)->GetModelType() != CharacterBase.ModelType.Human) return;
            var human = (Human*)character->DrawObject;
            var skeleton = human->Skeleton;
            if (skeleton == null) return;
            for (var i = 0; i < skeleton->PartialSkeletonCount && i < 1; ++i) {
                var partialSkeleton = &skeleton->PartialSkeletons[i];
                var animatedSkeleton = partialSkeleton->GetHavokAnimatedSkeleton(0);
                if (animatedSkeleton == null) continue;
                for (var animControl = 0; animControl < animatedSkeleton->AnimationControls.Length && animControl < 1; ++animControl) {
                    var control = animatedSkeleton->AnimationControls[animControl].Value;
                    if (control == null) continue;
                    var controlState = state.Find(s => s.SkeletonIndex == i && s.AnimationControlIndex == animControl);
                    if (controlState == null) continue;
                    control->LocalTime = controlState.Time;
                }
            }
        }, delayTicks: 1);
    }
}
