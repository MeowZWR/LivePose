using System.Collections.Generic;

namespace LivePose.Config;

public class PosingConfiguration
{
    // Overlay
    public bool AllowGizmoAxisFlip { get; set; } = true;
    public float BoneCircleDisplaySize { get; set; } = 5f;
    public float BoneCircleClickSize { get; set; } = 7.5f;
    public uint BoneCircleNormalColor { get; set; } = 0xFFFFFFFF;
    public uint BoneCircleInactiveColor { get; set; } = 0x55555555;
    public uint BoneCircleHoveredColor { get; set; } = 0xFFFF0073;
    public uint BoneCircleSelectedColor { get; set; } = 0xFFF82B56;
    public float SkeletonLineThickness { get; set; } = 0.010f;
    public uint SkeletonLineActiveColor { get; set; } = 0xFFFFFFFF;
    public uint SkeletonLineInactiveColor { get; set; } = 0x55555555;
    public bool ShowSkeletonLines { get; set; } = true;
    public bool HideGizmoWhenAdvancedPosingOpen { get; set; } = false;
    public bool HideToolbarWhenAdvandedPosingOpen { get; set; } = false;
    public bool HideSkeletonWhenGizmoActive { get; set; } = false;

    // Graphical Posing
    public bool GraphicalSidesSwapped { get; set; } = false;
    public bool ShowGenitaliaInAdvancedPoseWindow { get; set; } = false;

    // Undo / Redo
    public int UndoStackSize { get; set; } = 50;

    public bool FreezeActorOnPoseImport { get; set; } = false;

    public string[]? EnabledBoneCategories;
    
    public bool CursedMode { get; set; }
    public bool AutoSaveOnDestroy { get; set; } = true;
}
