using Brio.API;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using LivePose.Core;

namespace LivePose.IPC;

public class BrioService : BrioIPC {
    public override string Name => "Brio";
    public override int APIMajor => 3;
    public override int APIMinor => 0;

    public override bool IsAvailable => CheckStatus() == IPCStatus.Available;
    public override bool AllowIntegration => true;

    private readonly IDalamudPluginInterface _pluginInterface;

    private ApiVersion _apiVersion;
    private LoadPoseFromJson _loadPoseFromJson;
    private FreezeActor _freezeActor;
	private UnFreezeActor _unFreezeActor;
    private IsValidGPoseSession _isValidGPoseSession;
    private SetModelTransform _setModelTransform;
	private ResetPose _resetPose;


    public BrioService(IDalamudPluginInterface pluginInterface) {
        _pluginInterface = pluginInterface;

        _apiVersion = new ApiVersion(pluginInterface);
        _loadPoseFromJson = new LoadPoseFromJson(pluginInterface);
        _freezeActor = new FreezeActor(pluginInterface);
		_unFreezeActor = new UnFreezeActor(pluginInterface);
        _isValidGPoseSession = new IsValidGPoseSession(pluginInterface);
        _setModelTransform = new SetModelTransform(pluginInterface);
		_resetPose = new ResetPose(pluginInterface);
	}


    public bool SetPose(IGameObject? obj, string json) {
        if(obj == null) return false;
        if(!IsAvailable) return false;
        return _loadPoseFromJson.Invoke(obj, json);
    }

	public bool ResetPose(IGameObject? obj) {
		if (obj == null) return false;
		if (!IsAvailable) return false;
		return _resetPose.Invoke(obj);
	}

    public bool SetModelTransform(IGameObject? obj, Transform? transform) {
        if(obj == null) return false;
        if(transform == null) return false;
        if(!IsAvailable) return false;
        return _setModelTransform.Invoke(obj, transform?.Position, transform?.Rotation, transform?.Scale);
    }
    
    public bool IsValidGposeSession() => _isValidGPoseSession.Invoke();
    public bool FreezeActor(IGameObject obj) => _freezeActor.Invoke(obj);
	public bool UnFreezeActor(IGameObject obj) => _unFreezeActor.Invoke(obj);

    public override (int Major, int Minor) GetAPIVersion() => _apiVersion.Invoke();

    public override IDalamudPluginInterface GetPluginInterface() => _pluginInterface;
    
    public override void Dispose() {
        
    }
}
