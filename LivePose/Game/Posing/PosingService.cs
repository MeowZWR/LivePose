using System;
using Dalamud.Bindings.ImGuizmo;
using LivePose.Config;
using LivePose.Core;

namespace LivePose.Game.Posing;

public class PosingService : IDisposable
{
    public PosingOperation Operation { get; set; } = PosingOperation.Rotate;

    public PosingCoordinateMode CoordinateMode { get; set; } = PosingCoordinateMode.Local;

    public bool UniversalGizmoOperation { get; set; } = false;

    public BoneCategories BoneCategories { get; } = new();

    public BoneFilter OverlayFilter { get; }

    public PoseImporterOptions DefaultImporterOptions { get; }
    public PoseImporterOptions DefaultIPCImporterOptions { get; }

    public PoseImporterOptions SceneImporterOptions { get; }

    public PoseImporterOptions BodyOptions { get; }

    public PoseImporterOptions ExpressionOptions { get; }
    public PoseImporterOptions ExpressionOptions2 { get; }

    public PosingService(ConfigurationService configurationService)
    {
        OverlayFilter = new BoneFilter(this);
        OverlayFilter.DisableAll();

        
        foreach(var category in configurationService.Configuration.Posing.EnabledBoneCategories ?? []) {
            OverlayFilter.EnableCategory(category);
        }
        

        DefaultImporterOptions = new PoseImporterOptions(new BoneFilter(this), TransformComponents.Rotation);
        DefaultImporterOptions.BoneFilter.DisableCategory("weapon");
        DefaultImporterOptions.BoneFilter.DisableCategory("ex");

        DefaultIPCImporterOptions = new PoseImporterOptions(new BoneFilter(this), TransformComponents.All);

        SceneImporterOptions = new PoseImporterOptions(new BoneFilter(this), TransformComponents.All);

        BodyOptions = new PoseImporterOptions(new BoneFilter(this), TransformComponents.Rotation | TransformComponents.Position);
        BodyOptions.BoneFilter.DisableCategory("weapon");
        BodyOptions.BoneFilter.DisableCategory("head");
        BodyOptions.BoneFilter.DisableCategory("ears");
        BodyOptions.BoneFilter.DisableCategory("hair");
        BodyOptions.BoneFilter.DisableCategory("face");
        BodyOptions.BoneFilter.DisableCategory("eyes");
        BodyOptions.BoneFilter.DisableCategory("lips");
        BodyOptions.BoneFilter.DisableCategory("jaw");
        BodyOptions.BoneFilter.DisableCategory("head");
        BodyOptions.BoneFilter.DisableCategory("legacy");
        BodyOptions.BoneFilter.DisableCategory("ex");

        ExpressionOptions = new PoseImporterOptions(new BoneFilter(this), TransformComponents.All);
        ExpressionOptions.BoneFilter.DisableAll();
        ExpressionOptions.BoneFilter.EnableCategory("head");
        ExpressionOptions.BoneFilter.EnableCategory("ears");
        ExpressionOptions.BoneFilter.EnableCategory("hair");
        ExpressionOptions.BoneFilter.EnableCategory("face");
        ExpressionOptions.BoneFilter.EnableCategory("eyes");
        ExpressionOptions.BoneFilter.EnableCategory("lips");
        ExpressionOptions.BoneFilter.EnableCategory("jaw");

        ExpressionOptions2 = new PoseImporterOptions(new BoneFilter(this), TransformComponents.All);
        ExpressionOptions2.BoneFilter.DisableAll();
        ExpressionOptions2.BoneFilter.EnableCategory("head");
    }

    public PoseImporterOptions GetNewPoseImporterOptions(TransformComponents transformComponents)
        => new PoseImporterOptions(new BoneFilter(this), transformComponents);

    public void Dispose() { 
        BoneCategories.Dispose();
        
    }
}

public enum PosingCoordinateMode
{
    Local,
    World
}

public enum PosingOperation
{
    Translate,
    Rotate,
    Scale,
    Universal
}

public static class PosingExtensions
{
    public static ImGuizmoMode AsGizmoMode(this PosingCoordinateMode mode) => mode switch
    {
        PosingCoordinateMode.Local => ImGuizmoMode.Local,
        PosingCoordinateMode.World => ImGuizmoMode.World,
        _ => ImGuizmoMode.Local
    };

    public static ImGuizmoOperation AsGizmoOperation(this PosingOperation operation) => operation switch
    {
        PosingOperation.Translate => ImGuizmoOperation.Translate,
        PosingOperation.Rotate => ImGuizmoOperation.Rotate,
        PosingOperation.Scale => ImGuizmoOperation.Scale,
        PosingOperation.Universal => ImGuizmoOperation.Translate | ImGuizmoOperation.Rotate | ImGuizmoOperation.Scale,
        _ => ImGuizmoOperation.Rotate
    };
}
