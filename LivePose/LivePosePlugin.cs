using Dalamud.Plugin;

namespace LivePose;

internal class LivePosePlugin : IDalamudPlugin {

    private readonly LivePose livePose;
    
    public LivePosePlugin(IDalamudPluginInterface pluginInterface) {
        livePose = new LivePose(this, pluginInterface);
    }

    public void Dispose() {
        livePose.Dispose();
    }
}
