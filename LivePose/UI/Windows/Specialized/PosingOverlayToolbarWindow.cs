using System;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using LivePose.Capabilities.Posing;
using LivePose.Config;
using LivePose.Core;
using LivePose.Entities;
using LivePose.Game.Posing;
using LivePose.UI.Controls.Core;
using LivePose.UI.Controls.Editors;
using LivePose.UI.Controls.Stateless;
using LivePose.UI.Theming;
using OneOf.Types;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using LivePose.Capabilities.Actor;
using LivePose.Files;
using LivePose.IPC;

namespace LivePose.UI.Windows.Specialized;

public class PosingOverlayToolbarWindow : Window
{
    private readonly PosingOverlayWindow _overlayWindow;
    private readonly EntityManager _entityManager;
    private readonly PosingTransformWindow _overlayTransformWindow;
    private readonly PosingService _posingService;
    private readonly ConfigurationService _configurationService;
    private readonly IClientState _clientState;
    private readonly PosingGraphicalWindow _graphicalWindow;
    
    private readonly BoneSearchControl _boneSearchControl = new();

    private bool _pushedStyle = false;


    private const string _boneFilterPopupName = "livepose_bone_filter_popup";

    public PosingOverlayToolbarWindow(PosingOverlayWindow overlayWindow, EntityManager entityManager, PosingTransformWindow overlayTransformWindow, PosingService posingService, ConfigurationService configurationService, IClientState clientState, SettingsWindow settingsWindow, PosingGraphicalWindow graphicalWindow) : base($"{LivePose.Name} 叠加层###livepose_posing_overlay_toolbar_window", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse)
    {
        Namespace = "livepose_posing_overlay_toolbar_namespace";

        _overlayWindow = overlayWindow;
        _entityManager = entityManager;
        _overlayTransformWindow = overlayTransformWindow;
        _posingService = posingService;
        _configurationService = configurationService;
        _clientState = clientState;
        _graphicalWindow = graphicalWindow;

        TitleBarButtons =
        [
            new()
            {
                Icon = FontAwesomeIcon.Cog,
                Click = _ => settingsWindow.Toggle(),
                ShowTooltip = () => ImGui.SetTooltip("设置")
            }
        ];
        
        
        ShowCloseButton = false;
        
        
        
    }

    public override void PreOpenCheck()
    {
        IsOpen = _overlayWindow.IsOpen;
        

        if(UIManager.IsPosingGraphicalWindowOpen && _configurationService.Configuration.Posing.HideToolbarWhenAdvandedPosingOpen)
        {
            IsOpen = false;
        }

        base.PreOpenCheck();
    }

    public override bool DrawConditions()
    {
        if(_clientState.IsGPosing) return false;

        if(!_overlayWindow.IsOpen)
            return false;
        
        if(!_entityManager.TryGetCapabilityFromSelectedEntity<PosingCapability>(out var posing)) {
            return false;
        }

        if(posing.GameObject.ObjectIndex >= 2) return false;
        
        if(_clientState.LocalPlayer == null) return false;
        if(_clientState.LocalPlayer.StatusFlags.HasFlag(StatusFlags.InCombat)) return false;
        

        return base.DrawConditions();
    }

    public override void PreDraw()
    {
        base.PreDraw();
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        _pushedStyle = true;
    }

    public override void Draw()
    {
        if(_pushedStyle)
        {
            _pushedStyle = false;
            ImGui.PopStyleVar(2);
        }

        if(!_entityManager.TryGetCapabilityFromSelectedEntity<PosingCapability>(out var posing))
            return;
        
        DrawButtons(posing);


        
        
        
        if(_entityManager.TryGetCapabilityFromSelectedEntity<ActionTimelineCapability>(out var timelineCapability)) {
            
        }
        
        
        DrawBoneFilterPopup();
        
        
        
        
    }

    public override void PostDraw()
    {
        if(_pushedStyle)
        {
            _pushedStyle = false;
            ImGui.PopStyleVar(2);
        }

        base.PostDraw();
    }

    private void DrawButtons(PosingCapability posing)
    {

        float buttonSize = ImGui.GetTextLineHeight() * 3.2f;
        float buttonOperationSize = ImGui.GetTextLineHeight() * 2.4f;
        
        
        ImGui.PushStyleColor(ImGuiCol.Button, UIConstants.Transparent);

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            if(ImGui.Button($"{(_posingService.CoordinateMode == PosingCoordinateMode.Local ? FontAwesomeIcon.Globe.ToIconString() : FontAwesomeIcon.Atom.ToIconString())}###select_mode", new Vector2(buttonOperationSize)))
                _posingService.CoordinateMode = _posingService.CoordinateMode == PosingCoordinateMode.Local ? PosingCoordinateMode.World : PosingCoordinateMode.Local;
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip(_posingService.CoordinateMode == PosingCoordinateMode.Local ? "切换到世界坐标" : "切换到本地坐标");

        ImGui.SameLine();

        using(ImRaii.PushColor(ImGuiCol.Text, _overlayTransformWindow.IsOpen ? UIConstants.ToggleButtonActive : UIConstants.ToggleButtonInactive))
        {
            using(ImRaii.PushFont(UiBuilder.IconFont))
            {
                if(ImGui.Button($"{FontAwesomeIcon.LocationCrosshairs.ToIconString()}###toggle_transforms_window", new Vector2(buttonOperationSize)))
                    _overlayTransformWindow.IsOpen = !_overlayTransformWindow.IsOpen;
            }
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("切换变换窗口");
        
        ImGui.SameLine();

        using(ImRaii.PushColor(ImGuiCol.Text, _graphicalWindow.IsOpen ? UIConstants.ToggleButtonActive : UIConstants.ToggleButtonInactive))
        {
            using(ImRaii.PushFont(UiBuilder.IconFont)) {
                if(ImGui.Button($"{FontAwesomeIcon.PersonThroughWindow.ToIconString()}###toggle_advanced_window", new Vector2(buttonOperationSize)))
                    _graphicalWindow.IsOpen = !_graphicalWindow.IsOpen;
            }
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("切换高级姿势窗口");

        ImGui.SameLine();

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            if(ImGui.Button($"{FontAwesomeIcon.WindowClose.ToIconString()}###close_overlay", new Vector2(buttonOperationSize)))
                _overlayWindow.IsOpen = false;
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("关闭叠加层");

        ImGui.Separator();




        using(ImRaii.PushColor(ImGuiCol.Text, _posingService.Operation == PosingOperation.Translate ? UIConstants.ToggleButtonActive : UIConstants.ToggleButtonInactive))
        {
            using(ImRaii.PushFont(UiBuilder.IconFont))
            {
                if(ImGui.Button($"{FontAwesomeIcon.ArrowsUpDownLeftRight.ToIconString()}###select_position", new Vector2(buttonOperationSize)))
                    _posingService.Operation = PosingOperation.Translate;
            }
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("位置");

        ImGui.SameLine();


        using(ImRaii.PushColor(ImGuiCol.Text, _posingService.Operation == PosingOperation.Rotate ? UIConstants.ToggleButtonActive : UIConstants.ToggleButtonInactive))
        {
            using(ImRaii.PushFont(UiBuilder.IconFont))
            {
                if(ImGui.Button($"{FontAwesomeIcon.ArrowsSpin.ToIconString()}###select_rotate", new Vector2(buttonOperationSize)))
                    _posingService.Operation = PosingOperation.Rotate;
            }
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("旋转");

        ImGui.SameLine();

        using(ImRaii.PushColor(ImGuiCol.Text, _posingService.Operation == PosingOperation.Scale ? UIConstants.ToggleButtonActive : UIConstants.ToggleButtonInactive))
        {
            using(ImRaii.PushFont(UiBuilder.IconFont))
            {
                if(ImGui.Button($"{FontAwesomeIcon.ExpandAlt.ToIconString()}###select_scale", new Vector2(buttonOperationSize)))
                    _posingService.Operation = PosingOperation.Scale;
            }
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("缩放");

        ImGui.SameLine();

        using(ImRaii.PushColor(ImGuiCol.Text, _posingService.Operation == PosingOperation.Universal ? UIConstants.ToggleButtonActive : UIConstants.ToggleButtonInactive))
        {
            using(ImRaii.PushFont(UiBuilder.IconFont))
            {
                if(ImGui.Button($"{FontAwesomeIcon.Cubes.ToIconString()}###select_universal", new Vector2(buttonOperationSize)))
                {
                    _posingService.Operation = PosingOperation.Universal;
                }
            }
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("通用");

        ImGui.Separator();

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            if(ImGui.Button($"{FontAwesomeIcon.Bone.ToIconString()}###toggle_filter_window", new Vector2(buttonOperationSize)))
                ImGui.OpenPopup(_boneFilterPopupName);
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("骨骼过滤");

        ImGui.SameLine();

        PosingEditorCommon.DrawMirrorModeSelect(posing, new Vector2(buttonOperationSize));

        ImGui.SameLine();

        var bone = posing.Selected.Match(
          boneSelect => posing.SkeletonPosing.GetBone(boneSelect),
          _ => null,
          _ => null
       );

        var parentBone = bone?.Parent;

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            using(ImRaii.Disabled(parentBone == null))
            {
                if(ImGui.Button($"{FontAwesomeIcon.ArrowUp.ToIconString()}###select_parent", new Vector2(buttonOperationSize)))
                    posing.Selected = new BonePoseInfoId(parentBone!.Name, parentBone!.PartialId, PoseInfoSlot.Character);
            }
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("选择父骨骼");

        ImGui.SameLine();

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            using(ImRaii.Disabled(posing.Selected.Value is None))
            {
                if(ImGui.Button($"{FontAwesomeIcon.MinusSquare.ToIconString()}###clear_selected", new Vector2(buttonOperationSize)))
                    posing.ClearSelection();
            }
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("清除选择");
        
        
        // IK RED
        bool enabled = false;
        if(posing.Selected.Value is BonePoseInfoId boneId)
        {
            var bonePose = posing.SkeletonPosing.GetBonePose(boneId);
            var ik = bonePose.DefaultIK;
            enabled = ik.Enabled;
        }

        using(ImRaii.Disabled(!(bone?.EligibleForIK == true)))
        {
            using(ImRaii.PushColor(ImGuiCol.Button, ThemeManager.CurrentTheme.Accent.AccentColor, enabled))
            {
                if(ImGui.Button($"IK###bone_ik", new Vector2(buttonOperationSize)))
                    ImGui.OpenPopup("overlay_bone_ik");
            }
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("反向动力学");

        ImGui.SameLine();
        
        
        using(ImRaii.Disabled(!posing.SkeletonPosing.PoseInfo.HasIKStacks)) {
            if(ImGui.Button($"IK###clear_ik", new Vector2(buttonOperationSize))) {
                var pose = new PoseFile();
                posing.SkeletonPosing.ExportSkeletonPose(pose);
                foreach(var p in pose.Bones.Keys) {
                    var bBone = posing.SkeletonPosing.GetBone(p, PoseInfoSlot.Character);
                    if (bBone == null) continue;
                    var bonePoseInfo = posing.SkeletonPosing.GetBonePose(bBone);
                    
                    bonePoseInfo.ClearStacks();
                    bonePoseInfo.DefaultIK = BoneIKInfo.CalculateDefault(p);
                }
                
                posing.SkeletonPosing.ResetPose();
                posing.SkeletonPosing.ImportSkeletonPose(pose, new PoseImporterOptions(new BoneFilter(_posingService), TransformComponents.All, false));
            }
            
            var center = ImGui.GetItemRectMin() + ImGui.GetItemRectSize() / 2;
            var radius = MathF.Ceiling(ImGui.GetTextLineHeight() * 0.8f);
            var thickness = MathF.Ceiling(ImGui.GetTextLineHeight() * 0.1f);
            ImGui.GetWindowDrawList().AddCircle(center, radius, ImGui.GetColorU32(ImGuiCol.Text) & 0x80FFFFFF, 16, thickness);
            var offset = (radius - thickness) / MathF.Sqrt(2.0f);
            var lineStart = center + new Vector2(-offset, -offset);
            var lineEnd   = center + new Vector2(offset, offset);
            ImGui.GetWindowDrawList().AddLine(lineStart, lineEnd, 0x400000FF, thickness);
        }
        
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("重置反向动力学");

        ImGui.SameLine();
        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            if(ImGui.Button($"{FontAwesomeIcon.Search.ToIconString()}###bone_search", new Vector2(buttonOperationSize)))
                ImGui.OpenPopup("overlay_bone_search_popup");
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("骨骼搜索");

        if(_entityManager.TryGetCapabilityFromSelectedEntity<ActionTimelineCapability>(out var timelineCapability)) {
            ImGui.SameLine();



            using(ImRaii.PushColor(ImGuiCol.Text, timelineCapability.SpeedMultiplierOverride == 0 ? UIConstants.ToggleButtonActive : UIConstants.ToggleButtonInactive)) 
            using(ImRaii.PushFont(UiBuilder.IconFont)) {
                if(ImGui.Button($"{FontAwesomeIcon.Snowflake.ToIconString()}###freeze_toggle", new Vector2(buttonOperationSize))) {
                    if(timelineCapability.SpeedMultiplierOverride == 0) {
                        timelineCapability.ResetOverallSpeedOverride();
                    } else {
                        timelineCapability.SetOverallSpeedOverride(0);
                    }

                    if(timelineCapability.GameObject.ObjectIndex == 0 && LivePose.TryGetService<HeelsService>(out var service) && service.IsAvailable) {
                        service.SetPlayerPoseTag();
                    }
                }
            }
            

            if(ImGui.IsItemHovered())
                ImGui.SetTooltip($"{(timelineCapability.SpeedMultiplierOverride == 0 ? "解冻" : "冻结")}角色");

        }

        ImGui.Separator();

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            using(ImRaii.Disabled(!posing.CanUndo))
            {
                if(ImGui.Button($"{FontAwesomeIcon.Backward.ToIconString()}###undo_pose", new Vector2(buttonSize)))
                {
                    posing.Undo();
                }
            }
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("撤销");

        ImGui.SameLine();

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            using(ImRaii.Disabled(!posing.CanRedo))
            {
                if(ImGui.Button($"{FontAwesomeIcon.Forward.ToIconString()}###redo_pose", new Vector2(buttonSize)))
                {
                    posing.Redo();
                }
            }
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("重做");

        ImGui.SameLine();

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            using(ImRaii.Disabled(!posing.HasOverride))
            {
                if(ImGui.Button($"{FontAwesomeIcon.Undo.ToIconString()}###reset_pose", new Vector2(buttonSize)))
                    posing.Reset(true, false);
            }
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("重置姿势");

        ImGui.Separator();

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            if(ImGui.Button($"{FontAwesomeIcon.FileImport.ToIconString()}###import_pose", new Vector2(buttonSize)))
                ImGui.OpenPopup("DrawImportPoseMenuPopup");
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("导入姿势");

        FileUIHelpers.DrawImportPoseMenuPopup(posing, false);

        ImGui.SameLine();

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            if(ImGui.Button($"{FontAwesomeIcon.FileExport.ToIconString()}###export_pose", new Vector2(buttonSize)))
                FileUIHelpers.ShowExportPoseModal(posing);
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("导出姿势");

        ImGui.SameLine();

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            if(ImGui.Button($"{FontAwesomeIcon.Cog.ToIconString()}###import_options", new Vector2(buttonSize)))
                ImGui.OpenPopup("import_options_popup_pose_tooblar");
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("导入选项");

        ImGui.PopStyleColor();

        using(var popup = ImRaii.Popup("import_options_popup_pose_tooblar"))
        {
            if(popup.Success)
            {
                PosingEditorCommon.DrawImportOptionEditor(_posingService.DefaultImporterOptions);
            }
        }

        using(var popup = ImRaii.Popup("overlay_bone_search_popup"))
        {
            if(popup.Success)
            {
                _boneSearchControl.Draw("overlay_bone_search", posing);
            }
        }

        using(var popup = ImRaii.Popup("overlay_bone_ik"))
        {
            if(popup.Success)
            {
                if(posing.Selected.Value is BonePoseInfoId id)
                {
                    var info = posing.SkeletonPosing.GetBonePose(id);
                    BoneIKEditor.Draw(info, posing);
                }
            }
        }
    }

    private void DrawBoneFilterPopup() {
        using var popup = ImRaii.Popup(_boneFilterPopupName);
        if(!popup.Success) return;

        if(!PosingEditorCommon.DrawBoneFilterEditor(_posingService.OverlayFilter)) return;
        
        _configurationService.Configuration.Posing.EnabledBoneCategories = _posingService.OverlayFilter.AllCategories.Where(_posingService.OverlayFilter.IsCategoryEnabled).Select(c => c.Id).ToArray();
        _configurationService.Save();
    }
}
