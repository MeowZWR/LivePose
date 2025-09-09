using System;
using System.Threading;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using LivePose.Entities;

namespace LivePose.IPC;

public class HeelsService : BrioIPC {
    public override string Name => "Simple Heels";

    public override bool IsAvailable => CheckStatus()  == IPCStatus.Available;

    public override bool AllowIntegration => true;
    
    public override (int Major, int Minor) GetAPIVersion() {
        try {
            return _apiVersion.InvokeFunc();
        } catch {
            return (-1, -1);
        }
    }
    public override IDalamudPluginInterface GetPluginInterface() {
        return _pluginInterface;
    }

    public override int APIMajor => 2;
    public override int APIMinor => 4;
    
    private ICallGateSubscriber<(int, int)> _apiVersion;
    private static ICallGateSubscriber<int, string, string, object?>? _setTag;
    private static ICallGateSubscriber<int, string, string?>? _getTag;
    private static ICallGateSubscriber<int, string, object?>? _removeTag;
    private static ICallGateSubscriber<int, string, string?, object?>? _tagChanged;

    private IDalamudPluginInterface _pluginInterface;
    private IPluginLog _pluginLog;
    private EntityManager _entityManager;
    private IClientState _clientState;
    private IFramework _framework;
    private IpcService _ipcService;
    
    public HeelsService(IDalamudPluginInterface pluginInterface, IPluginLog pluginLog, IClientState clientState, EntityManager entityManager, IFramework framework, IpcService ipcService) {

        _pluginInterface = pluginInterface;
        _pluginLog = pluginLog;
        _entityManager = entityManager;
        _clientState = clientState;
        _framework = framework;
        _ipcService = ipcService;
        
        _apiVersion = pluginInterface.GetIpcSubscriber<(int, int)>("SimpleHeels.ApiVersion");
        _setTag = pluginInterface.GetIpcSubscriber<int, string, string, object?>("SimpleHeels.SetTag");
        _getTag = pluginInterface.GetIpcSubscriber<int, string, string?>("SimpleHeels.GetTag");
        _removeTag = pluginInterface.GetIpcSubscriber<int, string, object?>("SimpleHeels.RemoveTag");
        _tagChanged = pluginInterface.GetIpcSubscriber<int, string, string?, object?>("SimpleHeels.TagChanged");
        
        _pluginLog.Debug("Subscribing to SimpleHeels.TagChanged");
        _tagChanged.Subscribe(OnTagChanged);
        
        _framework.RunOnTick(SetPlayerPoseTag, delayTicks: 10);
        
        _framework.RunOnTick(() => {
            for(ushort i = 2; i < 200; i++) {
                var tag = _getTag.InvokeFunc(i, "LivePose");
                if(tag != null) {
                    OnTagChanged(i, "LivePose", tag);
                }
            }
        }, delayTicks: 60);
    }


    private CancellationTokenSource? cancellationTokenSource;
    
    public void SetPlayerPoseTag() {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource = new CancellationTokenSource();

        var token = cancellationTokenSource.Token;

        _framework.RunOnTick(() => {
            if(token.IsCancellationRequested) return;
            
            LivePosePlugin.Log.Debug("Updating LivePose Tag for local player.");
            
            /*
           var obj = _clientState.LocalPlayer;

           if(obj == null || !_entityManager.TryGetEntity(new EntityId(obj), out var entity) || entity is not ActorEntity actorEntity) {
               _removeTag?.InvokeAction(0, "LivePose");
               return;
           }



           if (!actorEntity.TryGetCapability<SkeletonPosingCapability>(out var skeletonPosingCapability)) {
               _removeTag?.InvokeAction(0, "LivePose");
               return;
           }

           var data = new LivePoseData() { Pose = new Dictionary<string, List<Transform>>() };


           foreach(var b in skeletonPosingCapability.PoseInfo.StackCounts.Keys) {
               var bone = skeletonPosingCapability.GetBone(b, PoseInfoSlot.Character);
               if (bone == null) continue;

               var bonePose = skeletonPosingCapability.GetBonePose(bone);
               if (!bonePose.HasStacks) continue;

               var list = new List<Transform>();
               foreach(var p in bonePose.Stacks) {
                   list.Add(p.Transform);
               }

               data.Pose.Add(b, list);
           }

           if(data.IsDefault) {
               _removeTag?.InvokeAction(0, "LivePose");
               return;
           }

           */

            var data = _ipcService.GetPose(0);

            if(string.IsNullOrWhiteSpace(data)) {
                _removeTag?.InvokeAction(0, "LivePose");
                return;
            }
            
            _setTag?.InvokeAction(0, "LivePose", data);
            
        }, delay: TimeSpan.FromMilliseconds(500), cancellationToken: token);
    }
    

    private void OnTagChanged(int objectIndex, string tag, string? value) {
        if(objectIndex < 2 || objectIndex >= 200) return;
        if(tag != "LivePose") return;
        LivePosePlugin.Log.Debug($"LivePose tag Changed for Object#{objectIndex}");
        _ipcService.SetPose((ushort)objectIndex, value ?? string.Empty);
    }

    public override void Dispose() {
        cancellationTokenSource?.Cancel();
        _tagChanged?.Unsubscribe(OnTagChanged);
    }
}
