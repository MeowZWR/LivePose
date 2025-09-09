using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using LivePose.Capabilities.Actor;
using LivePose.Entities.Actor;
using LivePose.UI.Controls.Core;

namespace LivePose.UI.Controls.Editors;

public class ActorEditor
{
    public unsafe static void DrawSpawnMenu(ActorContainerEntity actorContainerEntity)
    {
        var hasCapability = actorContainerEntity.TryGetCapability<ActorContainerCapability>(out var capability);

        using(ImRaii.Disabled(hasCapability == false))
        {
            // DrawSpawnMenu(capability!);
        }
    }
}
