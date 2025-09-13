using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;

namespace LivePose.IPC;

public class BrioService : BrioIPC {
    public override string Name => "Brio";
    public override int APIMajor => 2;
    public override int APIMinor => 0;

    public override bool IsAvailable => CheckStatus() == IPCStatus.Available;
    public override bool AllowIntegration => true;

    private readonly IDalamudPluginInterface _pluginInterface;
    
    public const string ApiVersion_IPCName = "Brio.ApiVersion";
    private readonly ICallGateSubscriber<(int, int)> API_Version_IPC;
    
    public const string Actor_PoseLoadFromJson_IPCName = "Brio.Actor.Pose.LoadFromJson";
    private readonly ICallGateSubscriber<IGameObject, string, bool, bool> Actor_Pose_LoadFromJson_IPC;


    public BrioService(IDalamudPluginInterface pluginInterface) {
        _pluginInterface = pluginInterface;
        API_Version_IPC = pluginInterface.GetIpcSubscriber<(int, int)>(ApiVersion_IPCName);
        Actor_Pose_LoadFromJson_IPC = pluginInterface.GetIpcSubscriber<IGameObject, string, bool, bool>(Actor_PoseLoadFromJson_IPCName);
    }


    public bool SetPose(IGameObject obj, string json, bool legacy = false) {
        if(!IsAvailable) return false;
        return Actor_Pose_LoadFromJson_IPC.InvokeFunc(obj, json, legacy);
    }


    
    public override (int Major, int Minor) GetAPIVersion() => API_Version_IPC.InvokeFunc();

    public override IDalamudPluginInterface GetPluginInterface() => _pluginInterface;
    
    public override void Dispose() {
        
    }
}
