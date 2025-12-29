using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using LivePose.Capabilities.Posing;
using LivePose.Entities;
using LivePose.Game.Posing;
using LivePose.Resources;
using Lumina.Excel.Sheets;
using Action = System.Action;

namespace LivePose.UI.Windows;

public class SavedPoseWindow : Window {
    private readonly EntityManager _entityManager;
    private readonly TimelineIdentification _timelineIdentification;
    private readonly IDataManager _dataManager;
    
    public SavedPoseWindow(EntityManager entityManager, TimelineIdentification timelineIdentification, IDataManager dataManager) : base("LivePose - Saved Poses") {
        Size = new Vector2(400, 300);
        SizeCondition = ImGuiCond.FirstUseEver;

        SizeConstraints = new WindowSizeConstraints() {
            MinimumSize = new Vector2(400, 300),
            MaximumSize = new Vector2(800, 600)
        };
        
        _entityManager = entityManager;
        _timelineIdentification = timelineIdentification;
        _dataManager = dataManager;
    }

    public override void Draw() {
        if(!_entityManager.TryGetCapabilityFromSelectedEntity<PosingCapability>(out var posing)) return;
        
        ImCallback.TabBar("SavedPoseTabs", () => {
            ImCallback.TabItem("Body Animation Poses", () => DrawBodyPoseTab(posing));                
            ImCallback.TabItem("Expression Poses", () => DrawFacePoseTab(posing));                
            ImCallback.TabItem("Minion Poses", () => DrawMinionPoseTab(posing));                
        });
    }

    private void DrawBodyPoseTab(PosingCapability posing) {
        DrawPoseTable("Body Animation", posing, posing.SkeletonPosing.BodyPoses, _timelineIdentification.GetBodyPoseName, posing.SkeletonPosing.ActiveBodyTimelines);
    }
    
    private void DrawFacePoseTab(PosingCapability posing) {
        DrawPoseTable("Expression", posing, posing.SkeletonPosing.FacePoses, _timelineIdentification.GetExpressionName, posing.SkeletonPosing.ActiveFaceTimeline);
    }

    private string GetMinionName(uint minionId) {
        if(!_dataManager.GetExcelSheet<Companion>().TryGetRow(minionId, out var minion)) return $"Companion#{minionId}";
        return minion.Singular.ExtractText();
    }
    
    private void DrawMinionPoseTab(PosingCapability posing) {
        DrawPoseTable("Minion", posing, posing.SkeletonPosing.MinionPoses, GetMinionName, posing.SkeletonPosing.ActiveMinion);
    }
    
    private void DrawPoseTable<TKey>(string typeName, PosingCapability posing, Dictionary<TKey, PoseInfo> poses, Func<TKey, string> getName, TKey active) where TKey : notnull {
        using var c = ImRaii.Child("scrolling_pose_table", ImGui.GetContentRegionAvail());
        if(!c.Success) return;
        if(!poses.Any(p => p.Value.IsOverridden())) {
            ImGui.Text($"You have no active {typeName} poses.");
            return;
        }
        
        Action? action = null;
        
        if(ImGui.BeginTable("poseTable", 2, ImGuiTableFlags.Borders)) {
            ImGui.TableSetupColumn("Control", ImGuiTableColumnFlags.WidthFixed, 80 * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn($"{typeName}", ImGuiTableColumnFlags.WidthStretch);
            foreach(var (key, pose) in poses) {
                if (!pose.IsOverridden()) continue;
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                using(ImRaii.Disabled(active.Equals(key))) {
                    if(ImGui.Button($"Clear##{key}", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2))) {
                        action = () => {
                            poses.Remove(key);
                        };
                    }
                }
                
                ImGui.TableNextColumn();
                var name = getName(key);
                using(ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.ParsedGreen, active.Equals(key))) {
                    ImGui.Text($"{name}");
                }
            
                if(ImGui.IsItemHovered()) ImGui.SetTooltip($"{key}");
            }
            
            ImGui.EndTable();
        }

        action?.Invoke();
    }
}
