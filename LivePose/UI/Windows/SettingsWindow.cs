using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using LivePose.Config;
using LivePose.UI.Controls.Stateless;
using System.Numerics;
using Dalamud.Interface.Components;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using LivePose.Capabilities.Posing;
using LivePose.Entities;
using LivePose.Entities.Actor;
using LivePose.Entities.Core;
using LivePose.Game.Posing;
using LivePose.Resources;
using LivePose.UI.Controls.Editors;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Swan.Formatters;

namespace LivePose.UI.Windows;

public class SettingsWindow : Window
{
    private readonly ConfigurationService _configurationService;
    private readonly EntityManager _entityManager;
    private readonly IClientState _clientState;

    public SettingsWindow(ConfigurationService configurationService, EntityManager entityManager, IClientState clientState) : base($"{LivePose.Name} SETTINGS###livepose_settings_window", ImGuiWindowFlags.None)
    {
        Namespace = "livepose_settings_namespace";

        _configurationService = configurationService;
        _entityManager = entityManager;
        _clientState = clientState;

        Size = new Vector2(500, 550);
        SizeCondition = ImGuiCond.FirstUseEver;
        
        SizeConstraints = new WindowSizeConstraints() {
            MinimumSize = Size.Value,
            MaximumSize = new Vector2(500, float.MaxValue)
        };
        
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
                DrawCategoryTab();
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


    private string newCategoryName = string.Empty;
    private BoneCategoryTypes newCategoryType = BoneCategoryTypes.Exact;
    private string newBoneName = string.Empty;

    private BoneSearchControl boneSearchControl = new BoneSearchControl();
    
    
    private void DrawCategoryTab() {
        var categories = _configurationService.Configuration.BoneCategories?.Where(c => c.Id != "other").ToArray();
        if(categories == null) return;
        
        if(!ImGui.CollapsingHeader("Category Settings")) return;

        var btnSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing());
        
        var edited = false;
        
        using(ImRaii.Disabled(!(ImGui.GetIO().KeyShift && ImGui.GetIO().KeyAlt))) {
            if(ImGui.Button("Restore Defaults", btnSize)) {
                _configurationService.Configuration.BoneCategories = null;
                _configurationService.ApplyChange();
            }
        }
        if(!(ImGui.GetIO().KeyShift && ImGui.GetIO().KeyAlt) && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
            ImGui.SetTooltip("Hold SHIFT and ALT to confirm");
        }

        ImGui.Text("Categories:");
        using(ImRaii.PushIndent()) {
            for (var i = 0; i < categories.Length; i++) {
                
                var id = categories[i].Id;
                using(ImRaii.PushId(id)) {
                    var name = categories[i].Name;
                    if(ImGuiComponents.IconButton(FontAwesomeIcon.Trash)) {
                        categories = categories.Where(c => c.Id != id).ToArray();
                        edited = true;
                        i--;
                        continue;
                    }
                    ImGui.SameLine(0, 0);
                    
                    if(ImGuiComponents.IconButton(FontAwesomeIcon.Copy)) {
                        ImGui.SetClipboardText(JsonConvert.SerializeObject(categories[i], ImGui.GetIO().KeyAlt ? Formatting.Indented : Formatting.None, new JsonSerializerSettings { Converters = [ new StringEnumConverter() ]}));
                    }
                    ImGui.SameLine(0, 0);
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);

                    if(ImGui.BeginCombo($"##Category_{categories[i].Id}", name, ImGuiComboFlags.HeightLargest | ImGuiComboFlags.NoArrowButton)) {
                        if(ImGui.InputText($"Category Name##{categories[i].Id}", ref name)) {
                            categories[i] = categories[i] with { Name = name };
                            edited = true;
                        }

                        if(ImGui.BeginCombo($"Category Type##{categories[i].Id}", $"{categories[i].Type.GetAttribute<DescriptionAttribute>()?.Description ?? categories[i].Type.ToString()}")) {
                            foreach(var e in Enum.GetValues<BoneCategoryTypes>()) {
                                if(ImGui.Selectable(e.GetAttribute<DescriptionAttribute>()?.Description ?? e.ToString(), categories[i].Type == e)) {
                                    categories[i] = categories[i] with { Type = e };
                                    edited = true;
                                }
                            }
                            
                            ImGui.EndCombo();
                        }
                        
                        for(var j = 0; j < categories[i].Bones.Count; j++) {
                            var t = categories[i].Bones[j];
                            if(ImGui.InputText($"##boneEntry_{j}##category_{categories[i].Id}", ref t, 128, ImGuiInputTextFlags.CharsNoBlank)) {
                                categories[i].Bones[j] = t.Trim();
                                edited = true;
                            }
                        }


                        if(_clientState.LocalPlayer != null && _entityManager.TryGetEntity(new EntityId(_clientState.LocalPlayer), out var entity) && entity is ActorEntity actor && actor.TryGetCapability<PosingCapability>(out var posingCapability)) { 
                            if(ImGui.BeginCombo("##boneSearch", "", ImGuiComboFlags.NoPreview)) {
                                var c = categories[i];
                                if(ImGui.IsWindowAppearing()) {
                                    ImGui.SetKeyboardFocusHere();
                                }
                                boneSearchControl.Draw("boneSearchCategoryEditor", posingCapability, (bpii) => {
                                    c.Bones.Add(bpii.BoneName);
                                    edited = true;
                                });
                                ImGui.EndCombo();
                            }
                            
                            ImGui.SameLine(0, 0);
                        }
                        
                        
                        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                        if(ImGui.InputText($"##boneEntry_{categories[i].Bones.Count}##category_{categories[i].Id}", ref newBoneName, 128, ImGuiInputTextFlags.CharsNoBlank)) {
                            categories[i].Bones.Add(newBoneName);
                            newBoneName = string.Empty;
                            edited = true;
                        }

                        if(!ImGui.IsAnyItemActive()) {
                            if(categories[i].Bones.RemoveAll(string.IsNullOrWhiteSpace) > 0) {
                                edited = true;
                            }
                        }
                    
                        ImGui.EndCombo();
                    }
                    
                }
            }

            var addButtonClicked = false;
            using(ImRaii.Disabled(string.IsNullOrWhiteSpace(newCategoryName))) {
                if(ImGuiComponents.IconButton(FontAwesomeIcon.Plus)) {
                    addButtonClicked = true;
                }
            }
            
            ImGui.SameLine(0, 0);
            if(ImGuiComponents.IconButton(FontAwesomeIcon.Paste)) {

                try {
                    var json = ImGui.GetClipboardText();
                    var category = JsonConvert.DeserializeObject<BoneCategory>(json, new JsonSerializerSettings { Converters = [ new StringEnumConverter() ]});

                    if(category != null) {
                        if(categories.Any(c => c.Id == category.Id)) {
                            category = category with { Id = Guid.NewGuid().ToString() };
                        }
                        
                        categories = categories.Append(category).ToArray();
                        edited = true;
                    }

                } catch(Exception e) {
                    LivePose.Log.Error(e, "Error loading pasted bone category");
                }
            }
            ImGui.SameLine(0, 0);
            
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if(ImGui.InputTextWithHint($"###newCategoryName", "New Category", ref newCategoryName, 64, ImGuiInputTextFlags.EnterReturnsTrue) || addButtonClicked) {
                var guid = Guid.NewGuid().ToString();
                categories = categories.Append(new BoneCategory(guid, newCategoryName.Trim(), BoneCategoryTypes.Exact, [])).ToArray();
                edited = true;
                newCategoryName = string.Empty;
            }
        }

        
        if(edited) {
            var list = categories.ToList();
            list.RemoveAll(c => c.Id == "other");
            list.Add(new BoneCategory("other", Localize.GetNullable("bone_categories.other") ?? "Other", BoneCategoryTypes.Filter, []));
            _configurationService.Configuration.BoneCategories = list;
            _configurationService.ApplyChange();
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
