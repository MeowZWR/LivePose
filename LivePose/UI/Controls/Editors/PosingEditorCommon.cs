﻿using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using LivePose.Capabilities.Posing;
using LivePose.Core;
using LivePose.Game.Posing;
using LivePose.UI.Controls.Core;
using LivePose.UI.Controls.Stateless;
using LivePose.UI.Theming;
using OneOf.Types;
using System.Numerics;

namespace LivePose.UI.Controls.Editors;

public static class PosingEditorCommon
{
    public static void DrawSelectionName(PosingCapability posing)
    {
        ImGui.Text(posing.Selected.DisplayName);

        if(posing.Actor.IsProp == false)
        {
            ImGui.SetWindowFontScale(0.75f);
            ImGui.TextDisabled(posing.Selected.Subtitle);
            ImGui.SetWindowFontScale(1.0f);
        }

        BonePoseInfoId? selectedIsBone = posing.IsSelectedBone();
        using(ImRaii.PushColor(ImGuiCol.Text, UIConstants.GizmoRed))
        {
            if(selectedIsBone.HasValue)
            {
                Game.Posing.Skeletons.Bone? bone = posing.SkeletonPosing.GetBone(selectedIsBone.Value);
                if(bone != null && bone.Skeleton.IsValid && bone.Freeze)
                {
                    ImGui.Text("该骨骼的变换值已被冻结。");
                }
            }
        }
    }

    public static void DrawImportOptionEditor(PoseImporterOptions options, bool compact = false)
    {
        DrawBoneFilterEditor(options.BoneFilter);

        if(compact == false)
        {
            ImGui.Separator();

            var selected = options.TransformComponents.HasFlag(TransformComponents.Position);
            if(ImGui.Checkbox("位置", ref selected))
            {
                if(selected)
                    options.TransformComponents |= TransformComponents.Position;
                else
                    options.TransformComponents &= ~TransformComponents.Position;
            }

            selected = options.TransformComponents.HasFlag(TransformComponents.Rotation);
            if(ImGui.Checkbox("旋转", ref selected))
            {
                if(selected)
                    options.TransformComponents |= TransformComponents.Rotation;
                else
                    options.TransformComponents &= ~TransformComponents.Rotation;
            }

            selected = options.TransformComponents.HasFlag(TransformComponents.Scale);
            if(ImGui.Checkbox("缩放", ref selected))
            {
                if(selected)
                    options.TransformComponents |= TransformComponents.Scale;
                else
                    options.TransformComponents &= ~TransformComponents.Scale;
            }

            ImGui.Separator();

            selected = options.ApplyModelTransform;
            if(ImGui.Checkbox("应用模型变换", ref selected))
            {
                options.ApplyModelTransform = selected;
            }
        }
    }

    public static bool DrawBoneFilterEditor(BoneFilter filter) {
        var modified = false;
        if(ImBrio.FontIconButton("select_all", Dalamud.Interface.FontAwesomeIcon.Check, "全选"))
        {
            filter.EnableAll();
            modified = true;
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("select_none", Dalamud.Interface.FontAwesomeIcon.Minus, "全不选"))
        {
            filter.DisableAll();
            modified = true;
        }

        ImGui.Separator();

        foreach(var category in filter.AllCategories)
        {
            var isEnabled = filter.IsCategoryEnabled(category);
            if(ImGui.Checkbox(category.Name, ref isEnabled))
            {
                if(isEnabled)
                    filter.EnableCategory(category);
                else
                    filter.DisableCategory(category);
                modified = true;
            }
            if(ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                filter.EnableOnly(category);
                modified = true;
            }
        }

        return modified;
    }

    public static void DrawMirrorModeSelect(PosingCapability posing, Vector2 buttonSize)
    {
        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            var hasMirror = posing.Selected.Match(
                boneSelect => posing.SkeletonPosing.GetBonePose(boneSelect).GetMirrorBone() != null,
                _ => false,
                _ => false
            );

            using(ImRaii.Disabled(posing.Selected.Value is None || !hasMirror))
            {
                posing.Selected.Switch(
                    boneSelect =>
                    {
                        var poseInfo = posing.SkeletonPosing.GetBonePose(boneSelect);
                        switch(poseInfo.MirrorMode)
                        {
                            case PoseMirrorMode.None:
                                if(ImGui.Button($"{FontAwesomeIcon.Unlink.ToIconString()}###mirror_mode", buttonSize))
                                    poseInfo.MirrorMode = PoseMirrorMode.Copy;
                                break;
                            case PoseMirrorMode.Copy:
                                if(ImGui.Button($"{FontAwesomeIcon.Link.ToIconString()}###mirror_mode", buttonSize))
                                    poseInfo.MirrorMode = PoseMirrorMode.Mirror;
                                break;
                            case PoseMirrorMode.Mirror:
                                if(ImGui.Button($"{FontAwesomeIcon.YinYang.ToIconString()}###mirror_mode", buttonSize))
                                    poseInfo.MirrorMode = PoseMirrorMode.None;
                                break;
                        }
                    },
                    _ => { ImGui.Button($"{FontAwesomeIcon.Unlink.ToIconString()}###mirror_mode", buttonSize); },
                    _ => { ImGui.Button($"{FontAwesomeIcon.Unlink.ToIconString()}###mirror_mode", buttonSize); }
                );
            }
        }

        if(ImGui.IsItemHovered())
        {
            if(posing.Selected.Value is BonePoseInfoId poseInfo)
            {
                switch(posing.SkeletonPosing.GetBonePose(poseInfo).MirrorMode)
                {
                    case PoseMirrorMode.None:
                        ImGui.SetTooltip("Link: None");
                        break;
                    case PoseMirrorMode.Copy:
                        ImGui.SetTooltip("Link: Copy");
                        break;
                    case PoseMirrorMode.Mirror:
                        ImGui.SetTooltip("Link: Mirror");
                        break;
                }
            }
        }
    }

    public static void DrawIKSelect(PosingCapability posing, Vector2 buttonSize)
    {
        if(posing.Selected.Value is BonePoseInfoId boneId)
        {
            var bone = posing.SkeletonPosing.GetBone(boneId);
            bool isValid = bone != null && bone.Skeleton.IsValid && bone.EligibleForIK;

            if(isValid)
            {
                var bonePose = posing.SkeletonPosing.GetBonePose(boneId);

                var ik = bonePose.DefaultIK;
                bool enabled = ik.Enabled;

                using(ImRaii.PushColor(ImGuiCol.Button, ThemeManager.CurrentTheme.Accent.AccentColor, enabled))
                {
                    if(ImGui.Button("IK", buttonSize))
                        ImGui.OpenPopup("transform_ik_popup");

                    if(ImGui.IsItemHovered())
                        ImGui.SetTooltip("Inverse Kinematics");
                }

                using var popup = ImRaii.Popup("transform_ik_popup");
                {
                    if(popup.Success && bonePose != null)
                    {
                        BoneIKEditor.Draw(bonePose, posing);
                    }
                }

                return;
            }
        }

        ImGui.BeginDisabled();
        ImGui.Button("IK", buttonSize);
        ImGui.EndDisabled();
    }
}
