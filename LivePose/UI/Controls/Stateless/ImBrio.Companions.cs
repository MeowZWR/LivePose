using Dalamud.Bindings.ImGui;
using LivePose.Game.Types;
using System.Numerics;

namespace LivePose.UI.Controls.Stateless;
public static partial class ImBrio
{
    public static bool BorderedGameIcon(string id, CompanionRowUnion union, bool showText = true, ImGuiButtonFlags flags = ImGuiButtonFlags.MouseButtonLeft, Vector2? size = null)
    {
        var (description, icon) = union.Match(
           companion => ($"{companion.Singular}\n{companion.RowId}\nModel: {companion.Model.RowId}", companion.Icon),
           mount => ($"{mount.Singular}\n{mount.RowId}\nModel: {mount.ModelChara.RowId}", mount.Icon),
           ornament => ($"{ornament.Singular}\n{ornament.RowId}\nModel: {ornament.Model}", ornament.Icon),
           none => ("None", (uint)0)
       );

        bool wasClicked = false;

        if(!showText)
        {
            description = string.Empty;
        }

        wasClicked = BorderedGameIcon(id, icon, "Images.UnknownIcon.png", description, flags, size);

        return wasClicked;
    }
}
