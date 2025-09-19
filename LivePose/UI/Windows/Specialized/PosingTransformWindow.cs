using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using LivePose.Capabilities.Posing;
using LivePose.Core;
using LivePose.Entities;
using LivePose.Game.Posing;
using LivePose.UI.Controls.Editors;
using LivePose.UI.Controls.Stateless;
using OneOf.Types;
using System.Numerics;
using Dalamud.Plugin.Services;
using LivePose.IPC;
using CameraManager = FFXIVClientStructs.FFXIV.Client.Game.Control.CameraManager;

namespace LivePose.UI.Windows.Specialized;

public class PosingTransformWindow : Window
{
    private readonly EntityManager _entityManager;
    private readonly PosingService _posingService;
    private readonly IClientState _clientState;
    private readonly PosingTransformEditor _posingTransformEditor = new();

    private Matrix4x4? _trackingMatrix;

    public PosingTransformWindow(EntityManager entityManager, PosingService posingService, IClientState clientState) : base($"{LivePose.Name} - 变换###livepose_transform_window", ImGuiWindowFlags.AlwaysAutoResize)
    {
        Namespace = "livepose_transform_namespace";

        _entityManager = entityManager;
        _posingService = posingService;
        _clientState = clientState;

        SizeConstraints = new WindowSizeConstraints
        {
            MaximumSize = new Vector2(350, 850),
            MinimumSize = new Vector2(200, 150)
        };

    }

    public override bool DrawConditions()
    {
        if(_clientState.IsGPosing) return false;
        
        if(!_entityManager.TryGetCapabilityFromSelectedEntity<PosingCapability>(out var posing)) {
            return false;
        }
        
        return posing.GameObject.ObjectIndex < 2;
    }

    public override void Draw()
    {
        if(!_entityManager.TryGetCapabilityFromSelectedEntity<PosingCapability>(out var posing))
        {
            return;
        }

        WindowName = $"{LivePose.Name} 变换 - {posing.Entity.FriendlyName}###livepose_transform_window";

        PosingEditorCommon.DrawSelectionName(posing);

        DrawButtons(posing);
        
        ImGui.Separator();
        
        if(posing.Selected.IsT0) {
            DrawGizmo();
        } else {
            if(ImBrio.FontIconButton((_posingService.CoordinateMode == PosingCoordinateMode.Local ? FontAwesomeIcon.Globe : FontAwesomeIcon.Atom)))
                _posingService.CoordinateMode = _posingService.CoordinateMode == PosingCoordinateMode.Local ? PosingCoordinateMode.World : PosingCoordinateMode.Local;
            ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().X));
        }

        ImGui.Separator();

        _posingTransformEditor.Draw("overlay_transforms_edit", posing);

    }

    private static void DrawButtons(PosingCapability posing)
    {
        float buttonWidth = (ImGui.GetContentRegionAvail().X - (ImGui.GetStyle().ItemSpacing.X * 3f)) / 4f;

        // Mirror mode
        PosingEditorCommon.DrawMirrorModeSelect(posing, new Vector2(buttonWidth, 0));

        // IK
        ImGui.SameLine();
        PosingEditorCommon.DrawIKSelect(posing, new Vector2(buttonWidth, 0));

        // Select Parent
        ImGui.SameLine();
        var parentBone = posing.Selected.Match(
               boneSelect => posing.SkeletonPosing.GetBone(boneSelect)?.GetFirstVisibleParent(),
               _ => null,
               _ => null
        );


        using(ImRaii.Disabled(parentBone == null))
        {
            if(ImBrio.FontIconButton(FontAwesomeIcon.LevelUpAlt, new Vector2(buttonWidth, 0)))
                posing.Selected = new BonePoseInfoId(parentBone!.Name, parentBone!.PartialId, PoseInfoSlot.Character);
        }

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("选择父骨骼");

        // Clear Selection
        ImGui.SameLine();
        using(ImRaii.Disabled(posing.Selected.Value is None))
        {
            if(ImGui.Button($"Clear###clear_selected", new Vector2(buttonWidth, 0)))
                posing.ClearSelection();
        }

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("清除选择");
    }

    private unsafe void DrawGizmo()
    {
        var selectedEntity = _entityManager.SelectedEntity;

        if(selectedEntity == null)
            return;

        if(!selectedEntity.TryGetCapability<PosingCapability>(out var posing))
            return;

        var camera = CameraManager.Instance()->GetActiveCamera();
        if(camera == null)
            return;

        var selected = posing.Selected;
        

        Game.Posing.Skeletons.Bone? selectedBone = null;

        Matrix4x4? targetMatrix = selected.Match<Matrix4x4?>(
            (boneSelect) =>
            {
                var bone = posing.SkeletonPosing.GetBone(boneSelect);
                if(bone == null)
                    return null;

                if(!bone.Skeleton.IsValid)
                    return null;

                if(bone.IsHidden)
                    return null;

                var charaBase = bone.Skeleton.CharacterBase;
                if(charaBase == null)
                    return null;

                selectedBone = bone;
                return bone.LastTransform.ToMatrix() * new Transform()
                {
                    Position = (Vector3)charaBase->CharacterBase.DrawObject.Object.Position,
                    Rotation = (Quaternion)charaBase->CharacterBase.DrawObject.Object.Rotation,
                    Scale = (Vector3)charaBase->CharacterBase.DrawObject.Object.Scale * charaBase->ScaleFactor
                }.ToMatrix();
            },
            _ => null,
            _ => null
        );

        if(targetMatrix == null)
            return;
        var matrix = _trackingMatrix ?? targetMatrix.Value;
        var originalMatrix = matrix;


        if(ImBrio.FontIconButton((_posingService.CoordinateMode == PosingCoordinateMode.Local ? FontAwesomeIcon.Globe : FontAwesomeIcon.Atom)))
            _posingService.CoordinateMode = _posingService.CoordinateMode == PosingCoordinateMode.Local ? PosingCoordinateMode.World : PosingCoordinateMode.Local;

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip(_posingService.CoordinateMode == PosingCoordinateMode.World ? "切换到本地坐标" : "切换到世界坐标");


        Vector2 gizmoSize = new(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().X);

        if(ImBrioGizmo.DrawRotation(ref matrix, gizmoSize, _posingService.CoordinateMode == PosingCoordinateMode.World))
        {
            if(!(selectedBone != null && selectedBone.Freeze))
                _trackingMatrix = matrix;
        }

        if(_trackingMatrix.HasValue) {
            selected.Switch(
                boneSelect => {
                    posing.SkeletonPosing.GetBonePose(boneSelect).Apply(_trackingMatrix.Value.ToTransform(), originalMatrix.ToTransform());
                   
                },
                _ => {},
                _ => {}
            );
            
            if(posing.GameObject.ObjectIndex == 0) {
                if(LivePose.TryGetService<HeelsService>(out var service) && service.IsAvailable) {
                    service.SetPlayerPoseTag();
                }
            }
        }
            

        if(!ImBrioGizmo.IsUsing() && _trackingMatrix.HasValue)
        {
            posing.Snapshot(false, false);
            _trackingMatrix = null;
        }
    }
}
