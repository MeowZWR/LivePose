using Dalamud.Bindings.ImGui;
using LivePose.Capabilities.Actor;
using LivePose.UI.Controls;
using LivePose.UI.Widgets.Core;

namespace LivePose.UI.Widgets.Actor;

public class ActorLifetimeWidget(ActorLifetimeCapability capability) : Widget<ActorLifetimeCapability>(capability)
{
    public override string HeaderName => "Lifetime";

    public override WidgetFlags Flags => WidgetFlags.DrawPopup;
    
    public override void DrawPopup()
    {
        if(ImGui.MenuItem($"Rename {Capability.Actor.FriendlyName}###actorlifetime_rename"))
        {
            ImGui.CloseCurrentPopup();

            RenameActorModal.Open(Capability.Actor);
        }
    }
}
