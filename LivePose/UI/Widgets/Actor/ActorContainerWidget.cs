using Dalamud.Bindings.ImGui;
using LivePose.Capabilities.Actor;
using LivePose.Entities.Actor;
using LivePose.UI.Widgets.Core;
using System.Numerics;

namespace LivePose.UI.Widgets.Actor;

public class ActorContainerWidget(ActorContainerCapability capability) : Widget<ActorContainerCapability>(capability)
{
    public override string HeaderName => "Actors";
    public override WidgetFlags Flags
    {
        get
        {
            WidgetFlags flags = WidgetFlags.DefaultOpen | WidgetFlags.DrawBody;

            if(Capability.CanControlCharacters)
                flags |= WidgetFlags.CanHide;

            return flags;
        }
    }

    private ActorEntity? _selectedActor;

    public override void DrawBody()
    {
        if(ImGui.BeginListBox($"###actorcontainerwidget_{Capability.Entity.Id}_list", new Vector2(-1, 150)))
        {
            foreach(var child in Capability.Entity.Children)
            {
                if(child is ActorEntity actorEntity)
                {
                    bool isSelected = actorEntity.Equals(_selectedActor);
                    if(ImGui.Selectable($"{child.FriendlyName}###actorcontainerwidget_{Capability.Entity.Id}_item_{actorEntity.Id}", isSelected, ImGuiSelectableFlags.AllowDoubleClick))
                    {
                        _selectedActor = actorEntity;
                    }
                }
            }

            ImGui.EndListBox();
        }
    }
}
