using Dalamud.Plugin;

namespace LivePose;

public class LivePosePlugin(IDalamudPluginInterface pluginInterface) : IDalamudPlugin {

    private readonly LivePose livePose = new(pluginInterface);

    public void Dispose() {
        livePose.Dispose();
    }
}
