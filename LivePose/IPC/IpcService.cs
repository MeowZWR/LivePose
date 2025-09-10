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

    public string GetPose(ushort objectIndex) {
        if(objectIndex > 200) return string.Empty;
        var obj = _objectTable[objectIndex];
        if(obj == null || !_entityManager.TryGetEntity(new EntityId(obj), out var entity) || entity is not ActorEntity actorEntity) return string.Empty;
        if (!actorEntity.TryGetCapability<ActionTimelineCapability>(out var timelineCapability)) return string.Empty;
        if (!actorEntity.TryGetCapability<SkeletonPosingCapability>(out var skeletonPosingCapability)) return string.Empty;


        var data = new LivePoseData();
        
        foreach(var b in skeletonPosingCapability.PoseInfo.StackCounts.Keys) {
            var bone = skeletonPosingCapability.GetBone(b, PoseInfoSlot.Character);
            if (bone == null) continue;

            var bonePose = skeletonPosingCapability.GetBonePose(bone);
            if (!bonePose.HasStacks) continue;
            
            

            var list = new List<BonePoseData>();
            foreach(var p in bonePose.Stacks) {
                
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

            data.Pose.Add(b, list);
        }
        
        data.Frozen = timelineCapability.SpeedMultiplierOverride == 0;
        
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

        var livePoseData = LivePoseData.Deserialize(data);
        
        _framework.RunOnTick(() => {
            skeletonPosingCapability.ResetPose();
            if(livePoseData == null) {
                LivePose.Log.Warning("Pose is null");
                return;
            }
            LivePose.Log.Verbose($"Applying Pose to GameObject#{obj.ObjectIndex} => {data}");
            foreach(var (b, list) in livePoseData.Pose) {
                var bone = skeletonPosingCapability.GetBone(b, PoseInfoSlot.Character);
                if(bone == null) continue;
                var bonePose = skeletonPosingCapability.GetBonePose(bone);
                foreach(var s in list) {
                    bonePose.Apply(s.Transform, null, s.Propogate, TransformComponents.All, s.BoneIkInfo);
                }
            }

            if(livePoseData.Frozen) {
                timelineCapability.SetOverallSpeedOverride(0f);
            } else {
                timelineCapability.ResetOverallSpeedOverride();
            }
            
        }, delayTicks: 1);
    }
    
    public void Dispose()
    {
        DisposeIPC();
    }
}
