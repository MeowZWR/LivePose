using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using LivePose.Capabilities.Posing;
using LivePose.Game.Posing;
using LivePose.Game.Posing.Skeletons;
using OneOf.Types;
using System;
using System.Numerics;

namespace LivePose.UI.Controls.Editors;

public class BoneSearchControl
{
    private string _searchTerm = string.Empty;
    public void Draw(string id, PosingCapability posing, Action<BonePoseInfoId>? onClick = null) {
        onClick ??= (bpii) => posing.Selected = bpii; 
        
        ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, 10);

        using(ImRaii.PushId(id))
        {
            ImGui.SetNextItemWidth(-1);
            ImGui.InputText("###search_term", ref _searchTerm, 256);

            using(var child = ImRaii.Child("###bone_search_editor_child", new Vector2(400, ImGui.GetTextLineHeight() * 25f), true))
            {
                if(child.Success)
                {

                    bool rootSelected = posing.Selected.Value is None || posing.Selected.Value is ModelTransformSelection;
                    ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.OpenOnDoubleClick;

                    if(rootSelected)
                        flags |= ImGuiTreeNodeFlags.Selected;

                    using(var node = ImRaii.TreeNode("Model", flags))
                    {
                        if(node.Success)
                        {
                            if(posing.SkeletonPosing.CharacterSkeleton != null)
                            {
                                using(var skeleton = ImRaii.TreeNode("Character", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.OpenOnDoubleClick))
                                {
                                    if(skeleton.Success)
                                    {
                                        DrawBone(posing.SkeletonPosing.CharacterSkeleton.RootBone, posing, PoseInfoSlot.Character, onClick);
                                    }
                                }
                            }

                            if(posing.SkeletonPosing.MainHandSkeleton != null)
                            {
                                using(var skeleton = ImRaii.TreeNode("Main Hand", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.OpenOnDoubleClick))
                                {
                                    if(skeleton.Success)
                                    {
                                        DrawBone(posing.SkeletonPosing.MainHandSkeleton.RootBone, posing, PoseInfoSlot.MainHand, onClick);
                                    }
                                }
                            }

                            if(posing.SkeletonPosing.OffHandSkeleton != null)
                            {
                                using(var skeleton = ImRaii.TreeNode("Off Hand", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.OpenOnDoubleClick))
                                {
                                    if(skeleton.Success)
                                    {
                                        DrawBone(posing.SkeletonPosing.OffHandSkeleton.RootBone, posing, PoseInfoSlot.OffHand, onClick);
                                    }
                                }
                            }

                            if(posing.SkeletonPosing.OrnamentSkeleton != null) {
                                using(var skeleton = ImRaii.TreeNode("Fashion Accessory", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.OpenOnDoubleClick))
                                {
                                    if(skeleton.Success)
                                    {
                                        DrawBone(posing.SkeletonPosing.OrnamentSkeleton.RootBone, posing, PoseInfoSlot.Ornament, onClick);
                                    }
                                }
                            }

                            if(posing.SkeletonPosing.MinionSkeleton != null) {
                                using(var skeleton = ImRaii.TreeNode("Minion", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.OpenOnDoubleClick)) {
                                    if(skeleton.Success) {
                                        DrawBone(posing.SkeletonPosing.MinionSkeleton.RootBone, posing, PoseInfoSlot.Minion, onClick);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        ImGui.PopStyleVar();
    }

    private void DrawBone(Bone bone, PosingCapability posing, PoseInfoSlot slot, Action<BonePoseInfoId> onClick)
    {
        var bonePoseInfoId = new BonePoseInfoId(bone.Name, bone.PartialId, slot);

        bool selected = posing.Selected.Value is BonePoseInfoId selectedBonePoseInfoid && selectedBonePoseInfoid == bonePoseInfoId;

        bool leaf = bone.Children.Count == 0;

        ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.OpenOnDoubleClick;

        bool treeIncludesTerm = TreeIncludesTerm(bone, _searchTerm, false);

        if(leaf || !treeIncludesTerm)
            flags |= ImGuiTreeNodeFlags.Leaf;

        if(selected)
            flags |= ImGuiTreeNodeFlags.Selected;

        treeIncludesTerm = TreeIncludesTerm(bone, _searchTerm, true);
        if(!treeIncludesTerm)
            return;

        if(!bone.IsHidden)
        {
            using(var node = ImRaii.TreeNode($"{bone.FriendlyName}###{bonePoseInfoId}", flags | ImGuiTreeNodeFlags.SpanFullWidth)) {
                var clicked = node.Success && ImGui.IsItemClicked();
                ImGui.SameLine();
                ImGui.TextDisabled(bone.Name);
                
                
                if(node.Success)
                {
                    if(clicked || ImGui.IsItemClicked())
                    {
                        onClick(bonePoseInfoId);
                    }

                    foreach(var child in bone.Children)
                    {
                        DrawBone(child, posing, slot, onClick);
                    }
                }
            }
        }
        else
        {
            foreach(var child in bone.Children)
            {
                DrawBone(child, posing, slot, onClick);
            }
        }
    }

    private bool TreeIncludesTerm(Bone bone, string term, bool includeCurrent)
    {
        if(string.IsNullOrWhiteSpace(term))
            return true;

        if(includeCurrent)
            if(bone.FriendlyDescriptor.Contains(term, StringComparison.OrdinalIgnoreCase) || bone.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                return true;

        foreach(var child in bone.Children)
        {
            if(TreeIncludesTerm(child, term, true))
                return true;
        }

        return false;
    }
}
