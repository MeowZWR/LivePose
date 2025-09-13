using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImGuizmo;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using LivePose.Capabilities.Posing;
using LivePose.Config;
using LivePose.Core;
using LivePose.Entities;
using LivePose.Entities.Core;
using LivePose.Game.GPose;
using LivePose.Game.Posing;
using LivePose.UI.Controls.Editors;
using OneOf.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using LivePose.Game.Camera;
using LivePose.IPC;
using CameraManager = FFXIVClientStructs.FFXIV.Client.Game.Control.CameraManager;

namespace LivePose.UI.Windows.Specialized;

public class PosingOverlayWindow : Window, IDisposable
{

    private readonly EntityManager _entityManager;
    private readonly ConfigurationService _configurationService;
    private readonly PosingService _posingService;
    private readonly GPoseService _gPoseService;
    private readonly HistoryService _groupedUndoService;
    private readonly IClientState _clientState;

    private List<ClickableItem> _selectingFrom = [];
    private Transform? _trackingTransform;
    private readonly PosingTransformEditor _posingTransformEditor = new();
    private List<(EntityId id, PoseInfo info)>? _groupedPendingSnapshot = null;

    private const int _gizmoId = 142857;
    private const string _boneSelectPopupName = "livepose_bone_select_popup";

    public PosingOverlayWindow(EntityManager entityManager, HistoryService groupedUndoService, ConfigurationService configService, PosingService posingService, GPoseService gPoseService, IClientState clientState)
        : base("##livepose_posing_overlay_window", ImGuiWindowFlags.AlwaysAutoResize, true)
    {
        Namespace = "livepose_posing_overlay_namespace";

        IsOpen = false;
        _entityManager = entityManager;
        _configurationService = configService;
        _posingService = posingService;
        _gPoseService = gPoseService;
        _groupedUndoService = groupedUndoService;
        _clientState = clientState;

        _gPoseService.OnGPoseStateChange += OnGPoseStateChanged;
    }

    public override bool DrawConditions() {
        if(_clientState.IsGPosing) return false;
        if(_clientState.LocalPlayer == null) return false;
        if(_clientState.LocalPlayer.StatusFlags.HasFlag(StatusFlags.InCombat)) return false;
        
        if(!_entityManager.TryGetCapabilityFromSelectedEntity<PosingCapability>(out var posing)) {
            return false;
        }
        
        return posing.GameObject.ObjectIndex < 2;
    }

    public override void PreDraw()
    {
        base.PreDraw();
        ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(0, 0), ImGuiCond.Always);
        SizeCondition = ImGuiCond.Always;

        var io = ImGui.GetIO();
        Size = io.DisplaySize * ImGui.GetFontSize();

        Flags = ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoCollapse;

        ImGuizmo.SetID(_gizmoId);

        //if(_trackingTransform.HasValue)
        //{
        //    Flags &= ~ImGuiWindowFlags.NoInputs;
        //}
    }

    public override void Draw()
    {
        if(!_entityManager.TryGetCapabilityFromSelectedEntity<PosingCapability>(out var posing))
        {
            return;
        }

        DrawContent(posing);
    }

    public override void PostDraw()
    {
        ImGuizmo.SetID(0);
        base.PostDraw();
    }

    private unsafe void DrawContent(PosingCapability posing)
    {
        var overlayConfig = _configurationService.Configuration.Posing;
        var uiState = new OverlayUIState(overlayConfig);
        var clickables = new List<ClickableItem>();

        CalculateClickables(posing, uiState, overlayConfig, ref clickables);

        HandleSkeletonInput(posing, uiState, clickables);
        DrawPopup(posing);
        DrawSkeletonLines(uiState, overlayConfig, clickables);
        DrawSkeletonDots(uiState, overlayConfig, clickables);
        DrawGizmo(posing, uiState);
    }

    private unsafe void CalculateClickables(PosingCapability posing, OverlayUIState uiState, PosingConfiguration config, ref List<ClickableItem> clickables) {
        var camera = (BrioCamera*) CameraManager.Instance()->GetActiveCamera();
        if(camera == null)
            return;
        
        // Bone Transforms
        if(posing.Actor.IsProp == false)
        {
            foreach(var (skeleton, poseSlot) in posing.SkeletonPosing.Skeletons)
            {
                if(!skeleton.IsValid)
                    continue;

                var charaBase = skeleton.CharacterBase;
                if(charaBase == null)
                    continue;

                var modelMatrix = new Transform()
                {
                    Position = (Vector3)charaBase->CharacterBase.DrawObject.Object.Position,
                    Rotation = (Quaternion)charaBase->CharacterBase.DrawObject.Object.Rotation,
                    Scale = (Vector3)charaBase->CharacterBase.DrawObject.Object.Scale * charaBase->ScaleFactor
                }.ToMatrix();


                foreach(var bone in skeleton.Bones)
                {
                    if(!_posingService.OverlayFilter.IsBoneValid(bone, poseSlot) || bone.Name == "n_throw")
                        continue;

                    var boneWorldPosition = Vector3.Transform(bone.LastTransform.Position, modelMatrix);

                    if(camera->WorldToScreen(boneWorldPosition, out var boneScreen))
                    {
                        clickables.Add(new ClickableItem
                        {
                            Item = posing.SkeletonPosing.GetBonePose(bone).Id,
                            ScreenPosition = boneScreen,
                            Size = config.BoneCircleDisplaySize,
                            ClickSize = config.BoneCircleClickSize,
                        });

                        if(bone.Parent != null)
                        {
                            if(!_posingService.OverlayFilter.IsBoneValid(bone.Parent, poseSlot))
                                continue;

                            var parentWorldPosition = Vector3.Transform(bone.Parent.LastTransform.Position, modelMatrix);
                            if(camera->WorldToScreen(parentWorldPosition, out var parentScreen))
                            {
                                clickables.Last().ParentScreenPosition = parentScreen;
                            }

                        }
                    }
                }
            }
        }

        // Selection
        foreach(var clickable in clickables)
        {
            if(posing.Selected.Equals(clickable.Item))
                clickable.CurrentlySelected = true;
        }
    }

    private void HandleSkeletonInput(PosingCapability posing, OverlayUIState uiState, List<ClickableItem> clickables)
    {
        if(!uiState.SkeletonInputEnabled)
            return;

        var clicked = new List<ClickableItem>();
        var hovered = new List<ClickableItem>();

        foreach(var clickable in clickables)
        {
            if (clickable.Item == PosingSelectionType.ModelTransform) continue;
            var start = new Vector2(clickable.ScreenPosition.X - clickable.ClickSize, clickable.ScreenPosition.Y - clickable.ClickSize);
            var end = new Vector2(clickable.ScreenPosition.X + clickable.ClickSize, clickable.ScreenPosition.Y + clickable.ClickSize);
            if(ImGui.IsMouseHoveringRect(start, end))
            {
                hovered.Add(clickable);
                clickable.CurrentlyHovered = true;
                uiState.AnyClickableHovered = true;

                ImGui.SetNextFrameWantCaptureMouse(true);

                if(ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    clicked.Add(clickable);
                    clickable.WasClicked = true;
                    uiState.AnyClickableClicked = true;
                }
            }
        }

        if(clicked.Count != 0)
        {
            posing.Selected = clicked[0].Item;

            if(clicked.Count > 1)
            {
                _selectingFrom = clicked;
                ImGui.OpenPopup(_boneSelectPopupName);
            }
        }

        if(hovered.Count != 0 && clicked.Count == 0)
        {
            ImGui.SetNextWindowPos(ImGui.GetMousePos() + new Vector2(15, 10), ImGuiCond.Always);
            if(ImGui.Begin("gizmo_bone_select_preview", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoMove))
            {
                foreach(var hover in hovered)
                {
                    ImGui.BeginDisabled();
                    ImGui.Selectable($"{hover.Item.DisplayName}###selectable_{hover.GetHashCode()}", hover.CurrentlySelected);
                    ImGui.EndDisabled();
                }

                ImGui.End();
            }

            var wheel = ImGui.GetIO().MouseWheel;
            if(wheel != 0)
            {
                if(hovered.Count == 1)
                {
                    posing.Selected = hovered[0].Item;
                }
                else
                {
                    _selectingFrom = hovered;
                    ImGui.OpenPopup(_boneSelectPopupName);
                }
            }
        }
    }


    private void DrawPopup(PosingCapability posing)
    {
        using(var popup = ImRaii.Popup(_boneSelectPopupName))
        {
            if(popup.Success)
            {
                int selectedIndex = -1;
                foreach(var click in _selectingFrom)
                {
                    bool isSelected = posing.Selected == click.Item;
                    if(isSelected)
                        selectedIndex = _selectingFrom.IndexOf(click);

                    if(ImGui.Selectable($"{click.Item.DisplayName}###clickable_{click.GetHashCode()}", isSelected))
                    {
                        posing.Selected = click.Item;
                        _selectingFrom = [];
                        ImGui.CloseCurrentPopup();
                    }
                }

                var wheel = ImGui.GetIO().MouseWheel;
                if(wheel != 0)
                {
                    if(wheel < 0)
                    {
                        selectedIndex++;
                        if(selectedIndex >= _selectingFrom.Count)
                            selectedIndex = 0;
                    }
                    else
                    {
                        selectedIndex--;
                        if(selectedIndex < 0)
                            selectedIndex = _selectingFrom.Count - 1;
                    }

                    posing.Selected = _selectingFrom[selectedIndex].Item;
                }
            }
        }
    }

    private static void DrawSkeletonLines(OverlayUIState uiState, PosingConfiguration config, List<ClickableItem> clickables)
    {
        if(!uiState.DrawSkeletonLines)
            return;

        foreach(var clickable in clickables)
        {
            if(clickable.ParentScreenPosition.HasValue)
            {
                float thickness = config.SkeletonLineThickness;
                uint color = uiState.SkeletonLinesEnabled ? config.SkeletonLineActiveColor : config.SkeletonLineInactiveColor;



                if(Vector2.DistanceSquared(clickable.ParentScreenPosition.Value, clickable.ScreenPosition) >= clickable.Size * clickable.Size) {
                    ImGui.GetWindowDrawList().AddLine(
                        PointAlongLine(clickable.ParentScreenPosition.Value, clickable.ScreenPosition, clickable.Size),
                        PointAlongLine(clickable.ScreenPosition, clickable.ParentScreenPosition.Value, clickable.Size),
                        color, thickness
                    );
                }
                
        
            }
        }
    }
    
    private static Vector2 PointAlongLine(Vector2 start, Vector2 end, float distance)
    {
        Vector2 direction = end - start;
        Vector2 unit = Vector2.Normalize(direction);
        return start + unit * distance;
    }

    private void DrawSkeletonDots(OverlayUIState uiState, PosingConfiguration config, List<ClickableItem> clickables)
    {
        if(!uiState.DrawSkeletonDots)
            return;

        foreach(var clickable in clickables)
        {
            if (clickable.Item == PosingSelectionType.ModelTransform) continue;
            bool isFilled = clickable.CurrentlySelected || clickable.CurrentlyHovered;

            var color = config.BoneCircleNormalColor;

            if(clickable.CurrentlyHovered)
                color = config.BoneCircleHoveredColor;

            if(clickable.CurrentlySelected)
                color = config.BoneCircleSelectedColor;

            if(!uiState.SkeletonDotsEnabled)
                color = config.BoneCircleInactiveColor;

            if(isFilled)
                ImGui.GetWindowDrawList().AddCircleFilled(clickable.ScreenPosition, clickable.Size, color);
            else
                ImGui.GetWindowDrawList().AddCircle(clickable.ScreenPosition, clickable.Size, color);
        }
    }

    private unsafe void DrawGizmo(PosingCapability posing, OverlayUIState uiState)
    {
        if(!uiState.DrawGizmo)
            return;

        if(posing.Selected.Value is None)
            return;

        var camera = (BrioCamera*) CameraManager.Instance()->GetActiveCamera();
        if(camera == null)
            return;

        var selected = posing.Selected;

        Matrix4x4 projectionMatrix = camera->GetProjectionMatrix();
        Matrix4x4 worldViewMatrix = camera->GetViewMatrix();
        worldViewMatrix.M44 = 1;

        Transform currentTransform = Transform.Identity;
        Matrix4x4 modelMatrix = worldViewMatrix;

        Game.Posing.Skeletons.Bone? selectedBone = null;

        var shouldDraw = selected.Match(
            boneSelect =>
            {
                var bone = posing.SkeletonPosing.GetBone(boneSelect);
                if(bone == null)
                    return false;

                if(!_posingService.OverlayFilter.IsBoneValid(bone, boneSelect.Slot))
                {
                    return false;
                }

                currentTransform = bone.LastTransform;

                var charaBase = bone.Skeleton.CharacterBase;
                if(charaBase == null)
                    return false;

                selectedBone = bone;
                modelMatrix = new Transform()
                {
                    Position = (Vector3)charaBase->CharacterBase.DrawObject.Object.Position,
                    Rotation = (Quaternion)charaBase->CharacterBase.DrawObject.Object.Rotation,
                    Scale = Vector3.Clamp((Vector3)charaBase->CharacterBase.DrawObject.Object.Scale * charaBase->ScaleFactor, new Vector3(.5f), new Vector3(1.5f))
                }.ToMatrix();

                worldViewMatrix = Matrix4x4.Multiply(modelMatrix, worldViewMatrix);

                return true;
            },
            _ =>
            {
                return true;
            },
            _ => false
        );

        if(!shouldDraw)
            return;

        var lastObserved = _trackingTransform ?? currentTransform;

        var lastMatrix = lastObserved.ToMatrix();

        ImGuizmo.BeginFrame();
        var io = ImGui.GetIO();
        ImGuizmo.SetRect(0, 0, io.DisplaySize.X, io.DisplaySize.Y);
        ImGuizmo.SetOrthographic(false);
        ImGuizmo.AllowAxisFlip(_configurationService.Configuration.Posing.AllowGizmoAxisFlip);
        ImGuizmo.SetDrawlist();
        ImGuizmo.Enable(uiState.GizmoEnabled);

        Transform? newTransform = null;

        if(ImGuizmoExtensions.MouseWheelManipulate(ref lastMatrix))
        {
            if(!(selectedBone != null && selectedBone.Freeze))
            {
                newTransform = lastMatrix.ToTransform();
                _trackingTransform = newTransform;
            }
        }

        if(ImGuizmo.Manipulate(
            ref worldViewMatrix.M11,
            ref projectionMatrix.M11,
            _posingService.Operation.AsGizmoOperation(),
            _posingService.CoordinateMode.AsGizmoMode(),
            ref lastMatrix.M11
        ))
        {
            if(!(selectedBone != null && selectedBone.Freeze))
            {
                if(!(selectedBone != null && selectedBone.Freeze))
                {
                    newTransform = lastMatrix.ToTransform();
                    _trackingTransform = newTransform;
                }
            }
        }

        if(_trackingTransform.HasValue && !ImGuizmo.IsUsing())
        {
            if(_groupedPendingSnapshot != null && _groupedPendingSnapshot.Count > 0)
            {
                _groupedUndoService.Snapshot(_groupedPendingSnapshot);
                _groupedPendingSnapshot = null;
            }

            foreach(var eid in _entityManager.SelectedEntityIds)
            {
                if(!_entityManager.TryGetEntity(eid, out var ent))
                    continue;

                if(!ent.TryGetCapability<PosingCapability>(out var cap))
                    continue;

                cap.Snapshot(false, false);
            }

            _trackingTransform = null;
        }

        ImGuizmo.Enable(true);

        if(newTransform != null)
        {
            var delta = newTransform.Value.CalculateDiff(lastObserved);

            selected.Switch(
                bone =>
                {
                    posing.SkeletonPosing.GetBonePose(bone).Apply(newTransform.Value, lastObserved);
                },
                _ =>
                {
                    if(_groupedPendingSnapshot == null && ImGuizmo.IsUsing())
                    {
                        var list = new List<(EntityId, PoseInfo)>();
                        foreach(var id in _entityManager.SelectedEntityIds)
                        {
                            if(!_entityManager.TryGetEntity(id, out var ent))
                                continue;

                            if(!ent.TryGetCapability<PosingCapability>(out var cap))
                                continue;

                            list.Add((id, cap.SkeletonPosing.PoseInfo.Clone()));
                        }
                        _groupedPendingSnapshot = list;
                    }

                    foreach(var id in _entityManager.SelectedEntityIds)
                    {
                        if(!_entityManager.TryGetEntity(id, out var ent))
                            continue;

                        if(!ent.TryGetCapability<PosingCapability>(out var cap))
                            continue;
                    }
                },
                _ => { }
            );
            
            if(posing.GameObject.ObjectIndex == 0 && LivePose.TryGetService<HeelsService>(out var service) && service.IsAvailable) {
                service.SetPlayerPoseTag();
            }
        }
    }

    private void OnGPoseStateChanged(bool newState)
    {
            IsOpen = false;
    }

    public void Dispose()
    {
        _gPoseService.OnGPoseStateChange -= OnGPoseStateChanged;

        GC.SuppressFinalize(this);
    }

    private class OverlayUIState(PosingConfiguration configuration)
    {
        public bool PopupOpen = ImGui.IsPopupOpen(_boneSelectPopupName);
        public bool UsingGizmo = ImGuizmo.IsUsing();
        public bool HoveringGizmo = ImGuizmo.IsOver();
        public bool AnyActive = ImGui.IsAnyItemActive();
        public bool AnyWindowHovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow);

        public bool AnythingBusy => PopupOpen || UsingGizmo || AnyActive || AnyWindowHovered;

        public bool AnyClickableHovered = false;
        public bool AnyClickableClicked = false;

        public bool DrawSkeletonLines => configuration.ShowSkeletonLines && (!UsingGizmo || !configuration.HideSkeletonWhenGizmoActive);
        public bool DrawSkeletonDots => !UsingGizmo || !configuration.HideSkeletonWhenGizmoActive;
        public bool SkeletonLinesEnabled => !PopupOpen && !UsingGizmo;
        public bool SkeletonDotsEnabled => !PopupOpen && !UsingGizmo;
        public bool SkeletonInputEnabled => !AnythingBusy && DrawSkeletonDots && SkeletonDotsEnabled;

        public bool DrawGizmo => !(configuration.HideGizmoWhenAdvancedPosingOpen && UIManager.IsPosingGraphicalWindowOpen);
        public bool GizmoEnabled => !PopupOpen && !AnyClickableClicked && !AnyClickableHovered;
    }

    public class ClickableItem
    {
        public PosingSelectionType Item = null!;

        public Vector2 ScreenPosition;
        public Vector2? ParentScreenPosition = null;

        public float Size;
        public float ClickSize;
        public bool CurrentlySelected;
        public bool CurrentlyHovered;
        public bool WasClicked;
    }
}
