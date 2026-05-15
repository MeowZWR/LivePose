using System;

using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;

using LivePose.Game.GPose;

namespace LivePose.IPC;

public class KtisisService : BrioIPC {
	public override string Name => "Ktisis";
	public override bool IsAvailable => this.GetAPIVersion() is not (0, 0);
	public override bool AllowIntegration => true;
	public override int APIMajor => 1;
	public override int APIMinor => 0;
	public override (int Major, int Minor) GetAPIVersion() => _ktisisApiVersion?.InvokeFunc() ?? (0, 0);
	public override IDalamudPluginInterface GetPluginInterface() => _pluginInterface;

	private readonly IDalamudPluginInterface _pluginInterface;
	private readonly BrioService _brioService;
	private readonly GPoseService _gPoseService;
	private readonly IObjectTable _objectTable;
	private readonly IFramework _framework;

	private readonly ICallGateSubscriber<(int, int)>? _ktisisApiVersion;
	private readonly ICallGateSubscriber<bool, bool>? _ktisisPosingChanged;

	public KtisisService(IDalamudPluginInterface pluginInterface, BrioService brioService, GPoseService gPoseService, IObjectTable objectTable, IFramework framework) {
		this._pluginInterface = pluginInterface;
		this._brioService = brioService;
		this._gPoseService = gPoseService;
		this._objectTable = objectTable;
		this._framework = framework;

		this._ktisisApiVersion = this._pluginInterface.GetIpcSubscriber<(int, int)>("Ktisis.ApiVersion");
		this._ktisisPosingChanged = this._pluginInterface.GetIpcSubscriber<bool, bool>("Ktisis.PosingChanged");
		this._ktisisPosingChanged.Subscribe(this.PosingChanged);
	}

	private void PosingChanged(bool isPosing) {
		if(!isPosing) return;

		foreach(var index in this._gPoseService.PosedIndexes) {
			try {
				this._framework.RunOnTick(() => {
					this._brioService.ResetPose(this._objectTable[index]);
				}, delayTicks: 10);
			} catch(Exception ex) {
				LivePose.Log.Warning(ex, $"Error handling PosingChanged for Index {index}");
			}
		}

		this._gPoseService.PosedIndexes.Clear(); // clear indexes to prevent re-resetting if posing is disabled then enabled again
	}

	public override void Dispose() {
		this._ktisisPosingChanged?.Unsubscribe(this.PosingChanged);
	}
}
