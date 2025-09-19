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

    public SettingsWindow(ConfigurationService configurationService) : base($"{LivePose.Name} 设置###livepose_settings_window", ImGuiWindowFlags.NoResize)
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

                if(ImBrio.Button("关闭", FontAwesomeIcon.Times, new Vector2(100, 0)))
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
        if(ImGui.CollapsingHeader("叠加层", ImGuiTreeNodeFlags.DefaultOpen))
        {
            bool allowGizmoAxisFlip = _configurationService.Configuration.Posing.AllowGizmoAxisFlip;
            if(ImGui.Checkbox("允许变换器轴翻转", ref allowGizmoAxisFlip))
            {
                _configurationService.Configuration.Posing.AllowGizmoAxisFlip = allowGizmoAxisFlip;
                _configurationService.ApplyChange();
            }

            bool hideGizmoWhenAdvancedPosingOpen = _configurationService.Configuration.Posing.HideGizmoWhenAdvancedPosingOpen;
            if(ImGui.Checkbox("高级姿势时隐藏变换器", ref hideGizmoWhenAdvancedPosingOpen))
            {
                _configurationService.Configuration.Posing.HideGizmoWhenAdvancedPosingOpen = hideGizmoWhenAdvancedPosingOpen;
                _configurationService.ApplyChange();
            }

            bool hideToolbarWhenAdvancedPosingOpen = _configurationService.Configuration.Posing.HideToolbarWhenAdvandedPosingOpen;
            if(ImGui.Checkbox("高级姿势时隐藏工具栏", ref hideToolbarWhenAdvancedPosingOpen))
            {
                _configurationService.Configuration.Posing.HideToolbarWhenAdvandedPosingOpen = hideToolbarWhenAdvancedPosingOpen;
                _configurationService.ApplyChange();
            }

            bool showSkeletonLines = _configurationService.Configuration.Posing.ShowSkeletonLines;
            if(ImGui.Checkbox("显示骨架线", ref showSkeletonLines))
            {
                _configurationService.Configuration.Posing.ShowSkeletonLines = showSkeletonLines;
                _configurationService.ApplyChange();
            }

            bool hideSkeletonWhenGizmoActive = _configurationService.Configuration.Posing.HideSkeletonWhenGizmoActive;
            if(ImGui.Checkbox("变换器激活时隐藏骨架", ref hideSkeletonWhenGizmoActive))
            {
                _configurationService.Configuration.Posing.HideSkeletonWhenGizmoActive = hideSkeletonWhenGizmoActive;
                _configurationService.ApplyChange();
            }

            float lineThickness = _configurationService.Configuration.Posing.SkeletonLineThickness;
            if(ImGui.DragFloat("骨架线粗细", ref lineThickness, 0.01f, 0.01f, 20f))
            {
                _configurationService.Configuration.Posing.SkeletonLineThickness = lineThickness;
                _configurationService.ApplyChange();
            }

            float circleSize = _configurationService.Configuration.Posing.BoneCircleDisplaySize;
            if(ImGui.DragFloat("关节圆圈大小（显示）", ref circleSize, 0.01f, 0.01f, 20f))
            {
                if (circleSize > _configurationService.Configuration.Posing.BoneCircleClickSize) _configurationService.Configuration.Posing.BoneCircleClickSize = circleSize;
                
                _configurationService.Configuration.Posing.BoneCircleDisplaySize = circleSize;
                _configurationService.ApplyChange();
            }            

            float circleSizeClick = _configurationService.Configuration.Posing.BoneCircleClickSize;
            if(ImGui.DragFloat("关节圆圈大小（点击）", ref circleSizeClick, 0.01f, 0.01f, 20f))
            {
                if (circleSizeClick < _configurationService.Configuration.Posing.BoneCircleDisplaySize) circleSizeClick = _configurationService.Configuration.Posing.BoneCircleDisplaySize;
                _configurationService.Configuration.Posing.BoneCircleClickSize = circleSizeClick;
                _configurationService.ApplyChange();
            }

            Vector4 boneCircleNormalColor = ImGui.ColorConvertU32ToFloat4(_configurationService.Configuration.Posing.BoneCircleNormalColor);
            if(ImGui.ColorEdit4("关节圆圈常规颜色", ref boneCircleNormalColor, ImGuiColorEditFlags.NoInputs))
            {

                _configurationService.Configuration.Posing.BoneCircleNormalColor = ImGui.ColorConvertFloat4ToU32(boneCircleNormalColor);
                _configurationService.ApplyChange();
            }

            Vector4 boneCircleInactiveColor = ImGui.ColorConvertU32ToFloat4(_configurationService.Configuration.Posing.BoneCircleInactiveColor);
            if(ImGui.ColorEdit4("关节圆圈未激活颜色", ref boneCircleInactiveColor, ImGuiColorEditFlags.NoInputs))
            {

                _configurationService.Configuration.Posing.BoneCircleInactiveColor = ImGui.ColorConvertFloat4ToU32(boneCircleInactiveColor);
                _configurationService.ApplyChange();
            }

            Vector4 boneCircleHoveredColor = ImGui.ColorConvertU32ToFloat4(_configurationService.Configuration.Posing.BoneCircleHoveredColor);
            if(ImGui.ColorEdit4("关节圆圈悬停颜色", ref boneCircleHoveredColor, ImGuiColorEditFlags.NoInputs))
            {

                _configurationService.Configuration.Posing.BoneCircleHoveredColor = ImGui.ColorConvertFloat4ToU32(boneCircleHoveredColor);
                _configurationService.ApplyChange();
            }

            Vector4 boneCircleSelectedColor = ImGui.ColorConvertU32ToFloat4(_configurationService.Configuration.Posing.BoneCircleSelectedColor);
            if(ImGui.ColorEdit4("关节圆圈选中颜色", ref boneCircleSelectedColor, ImGuiColorEditFlags.NoInputs))
            {

                _configurationService.Configuration.Posing.BoneCircleSelectedColor = ImGui.ColorConvertFloat4ToU32(boneCircleSelectedColor);
                _configurationService.ApplyChange();
            }

            Vector4 skeletonLineActive = ImGui.ColorConvertU32ToFloat4(_configurationService.Configuration.Posing.SkeletonLineActiveColor);
            if(ImGui.ColorEdit4("骨架线激活颜色", ref skeletonLineActive, ImGuiColorEditFlags.NoInputs))
            {

                _configurationService.Configuration.Posing.SkeletonLineActiveColor = ImGui.ColorConvertFloat4ToU32(skeletonLineActive);
                _configurationService.ApplyChange();
            }

            Vector4 skeletonLineInactive = ImGui.ColorConvertU32ToFloat4(_configurationService.Configuration.Posing.SkeletonLineInactiveColor);
            if(ImGui.ColorEdit4("骨架线未激活颜色", ref skeletonLineInactive, ImGuiColorEditFlags.NoInputs))
            {

                _configurationService.Configuration.Posing.SkeletonLineInactiveColor = ImGui.ColorConvertFloat4ToU32(skeletonLineInactive);
                _configurationService.ApplyChange();
            }
        }
    }

    private void DrawPosingGeneralSection()
    {
        if(ImGui.CollapsingHeader("通用设置", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var undoStackSize = _configurationService.Configuration.Posing.UndoStackSize;
            if(ImGui.DragInt("撤销历史记录", ref undoStackSize, 1, 0, 100))
            {
                _configurationService.Configuration.Posing.UndoStackSize = undoStackSize;
                _configurationService.ApplyChange();
            }
        }
    }
    
    private void DrawAdvancedTab()
    {

        if(ImGui.IsWindowAppearing()) ImGui.SetNextItemOpen(false);
        if(ImGui.CollapsingHeader("重置设置"))
        {
            using(ImRaii.Disabled(!ImGui.GetIO().KeyShift))
            {
                if(ImGui.Button("恢复默认设置", new(170, 0)))
                {
                    _configurationService.Reset();
                }
            }
            
            ImGui.TextDisabled("按住 SHIFT 以确认。");
        }
    }
}
