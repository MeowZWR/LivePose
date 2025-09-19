using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using LivePose.Config;
using LivePose.UI.Controls.Stateless;
using System.Numerics;

namespace LivePose.UI.Windows;

public class SettingsWindow : Window
{
    private readonly ConfigurationService _configurationService;

    public SettingsWindow(ConfigurationService configurationService) : base($"{LivePose.Name} SETTINGS###livepose_settings_window", ImGuiWindowFlags.NoResize)
    {
        Namespace = "livepose_settings_namespace";

        _configurationService = configurationService;

        Size = new Vector2(500, 550);
    }

    private bool _isModal = false;
    public void OpenAsLibraryTab()
    {
        Flags = ImGuiWindowFlags.Modal | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize;
        IsOpen = true;

        BringToFront();

        _isModal = true;
    }

    public override void OnClose()
    {
        Flags = ImGuiWindowFlags.NoResize;
        _isModal = false;
    }
    
    public override void Draw()
    {
        using(ImRaii.PushId("livepose_settings"))
        {
            if(_isModal)
            {

                if(ImBrio.Button("Close", FontAwesomeIcon.Times, new Vector2(100, 0)))
                {
                    IsOpen = false;
                }
            }
            else
            {

                DrawPosingTab();
                DrawAdvancedTab();
            }
        }
    }
    
    private void DrawPosingTab()
    {
        DrawPosingGeneralSection();
        DrawOverlaySection();
    }

    private void DrawOverlaySection()
    {
        if(ImGui.CollapsingHeader("Overlay", ImGuiTreeNodeFlags.DefaultOpen))
        {
            bool allowGizmoAxisFlip = _configurationService.Configuration.Posing.AllowGizmoAxisFlip;
            if(ImGui.Checkbox("Allow Gizmo Axis Flip", ref allowGizmoAxisFlip))
            {
                _configurationService.Configuration.Posing.AllowGizmoAxisFlip = allowGizmoAxisFlip;
                _configurationService.ApplyChange();
            }

            bool hideGizmoWhenAdvancedPosingOpen = _configurationService.Configuration.Posing.HideGizmoWhenAdvancedPosingOpen;
            if(ImGui.Checkbox("Hide Gizmo while Advanced Posing", ref hideGizmoWhenAdvancedPosingOpen))
            {
                _configurationService.Configuration.Posing.HideGizmoWhenAdvancedPosingOpen = hideGizmoWhenAdvancedPosingOpen;
                _configurationService.ApplyChange();
            }

            bool hideToolbarWhenAdvancedPosingOpen = _configurationService.Configuration.Posing.HideToolbarWhenAdvandedPosingOpen;
            if(ImGui.Checkbox("Hide Toolbar while Advanced Posing", ref hideToolbarWhenAdvancedPosingOpen))
            {
                _configurationService.Configuration.Posing.HideToolbarWhenAdvandedPosingOpen = hideToolbarWhenAdvancedPosingOpen;
                _configurationService.ApplyChange();
            }

            bool showSkeletonLines = _configurationService.Configuration.Posing.ShowSkeletonLines;
            if(ImGui.Checkbox("Show Skeleton Lines", ref showSkeletonLines))
            {
                _configurationService.Configuration.Posing.ShowSkeletonLines = showSkeletonLines;
                _configurationService.ApplyChange();
            }

            bool hideSkeletonWhenGizmoActive = _configurationService.Configuration.Posing.HideSkeletonWhenGizmoActive;
            if(ImGui.Checkbox("Hide Skeleton when Gizmo Active", ref hideSkeletonWhenGizmoActive))
            {
                _configurationService.Configuration.Posing.HideSkeletonWhenGizmoActive = hideSkeletonWhenGizmoActive;
                _configurationService.ApplyChange();
            }

            float lineThickness = _configurationService.Configuration.Posing.SkeletonLineThickness;
            if(ImGui.DragFloat("Line Thickness", ref lineThickness, 0.01f, 0.01f, 20f))
            {
                _configurationService.Configuration.Posing.SkeletonLineThickness = lineThickness;
                _configurationService.ApplyChange();
            }

            float circleSize = _configurationService.Configuration.Posing.BoneCircleDisplaySize;
            if(ImGui.DragFloat("Circle Size (Display)", ref circleSize, 0.01f, 0.01f, 20f))
            {
                if (circleSize > _configurationService.Configuration.Posing.BoneCircleClickSize) _configurationService.Configuration.Posing.BoneCircleClickSize = circleSize;
                
                _configurationService.Configuration.Posing.BoneCircleDisplaySize = circleSize;
                _configurationService.ApplyChange();
            }            

            float circleSizeClick = _configurationService.Configuration.Posing.BoneCircleClickSize;
            if(ImGui.DragFloat("Circle Size (Click)", ref circleSizeClick, 0.01f, 0.01f, 20f))
            {
                if (circleSizeClick < _configurationService.Configuration.Posing.BoneCircleDisplaySize) circleSizeClick = _configurationService.Configuration.Posing.BoneCircleDisplaySize;
                _configurationService.Configuration.Posing.BoneCircleClickSize = circleSizeClick;
                _configurationService.ApplyChange();
            }

            Vector4 boneCircleNormalColor = ImGui.ColorConvertU32ToFloat4(_configurationService.Configuration.Posing.BoneCircleNormalColor);
            if(ImGui.ColorEdit4("Bone Circle Normal Color", ref boneCircleNormalColor, ImGuiColorEditFlags.NoInputs))
            {

                _configurationService.Configuration.Posing.BoneCircleNormalColor = ImGui.ColorConvertFloat4ToU32(boneCircleNormalColor);
                _configurationService.ApplyChange();
            }

            Vector4 boneCircleInactiveColor = ImGui.ColorConvertU32ToFloat4(_configurationService.Configuration.Posing.BoneCircleInactiveColor);
            if(ImGui.ColorEdit4("Bone Circle Inactive Color", ref boneCircleInactiveColor, ImGuiColorEditFlags.NoInputs))
            {

                _configurationService.Configuration.Posing.BoneCircleInactiveColor = ImGui.ColorConvertFloat4ToU32(boneCircleInactiveColor);
                _configurationService.ApplyChange();
            }

            Vector4 boneCircleHoveredColor = ImGui.ColorConvertU32ToFloat4(_configurationService.Configuration.Posing.BoneCircleHoveredColor);
            if(ImGui.ColorEdit4("Bone Circle Hovered Color", ref boneCircleHoveredColor, ImGuiColorEditFlags.NoInputs))
            {

                _configurationService.Configuration.Posing.BoneCircleHoveredColor = ImGui.ColorConvertFloat4ToU32(boneCircleHoveredColor);
                _configurationService.ApplyChange();
            }

            Vector4 boneCircleSelectedColor = ImGui.ColorConvertU32ToFloat4(_configurationService.Configuration.Posing.BoneCircleSelectedColor);
            if(ImGui.ColorEdit4("Bone Circle Selected Color", ref boneCircleSelectedColor, ImGuiColorEditFlags.NoInputs))
            {

                _configurationService.Configuration.Posing.BoneCircleSelectedColor = ImGui.ColorConvertFloat4ToU32(boneCircleSelectedColor);
                _configurationService.ApplyChange();
            }

            Vector4 skeletonLineActive = ImGui.ColorConvertU32ToFloat4(_configurationService.Configuration.Posing.SkeletonLineActiveColor);
            if(ImGui.ColorEdit4("Skeleton Active Color", ref skeletonLineActive, ImGuiColorEditFlags.NoInputs))
            {

                _configurationService.Configuration.Posing.SkeletonLineActiveColor = ImGui.ColorConvertFloat4ToU32(skeletonLineActive);
                _configurationService.ApplyChange();
            }

            Vector4 skeletonLineInactive = ImGui.ColorConvertU32ToFloat4(_configurationService.Configuration.Posing.SkeletonLineInactiveColor);
            if(ImGui.ColorEdit4("Skeleton Inactive Color", ref skeletonLineInactive, ImGuiColorEditFlags.NoInputs))
            {

                _configurationService.Configuration.Posing.SkeletonLineInactiveColor = ImGui.ColorConvertFloat4ToU32(skeletonLineInactive);
                _configurationService.ApplyChange();
            }
        }
    }

    private void DrawPosingGeneralSection()
    {
        if(ImGui.CollapsingHeader("General", ImGuiTreeNodeFlags.DefaultOpen)) {

            var curseMode = _configurationService.Configuration.Posing.CursedMode;
            if(ImGui.Checkbox("Cursed Mode", ref curseMode)) {
                _configurationService.Configuration.Posing.CursedMode = curseMode;
                _configurationService.ApplyChange();
            }
            
            
            var undoStackSize = _configurationService.Configuration.Posing.UndoStackSize;
            if(ImGui.DragInt("Undo History", ref undoStackSize, 1, 0, 100))
            {
                _configurationService.Configuration.Posing.UndoStackSize = undoStackSize;
                _configurationService.ApplyChange();
            }
        }
    }
    
    private void DrawAdvancedTab()
    {

        if(ImGui.IsWindowAppearing()) ImGui.SetNextItemOpen(false);
        if(ImGui.CollapsingHeader("Reset Settings"))
        {
            using(ImRaii.Disabled(!ImGui.GetIO().KeyShift))
            {
                if(ImGui.Button("Reset Settings to Default", new(170, 0)))
                {
                    _configurationService.Reset();
                }
            }
            
            ImGui.TextDisabled("Hold SHIFT to confirm.");
        }
    }
}
