using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using LivePose.Capabilities.Posing;
using LivePose.Entities;
using LivePose.Entities.Actor;
using LivePose.IPC;
using LivePose.Resources;
using Lumina.Excel.Sheets;

namespace LivePose.UI.Windows;

public unsafe class DebugWindow(TimelineIdentification timelineIdentification, IpcService ipcService) : Window($"{LivePose.Name} Debug") {
    private void ShowEntity(ActorEntity actorEntity) {
        if(actorEntity.TryGetCapability<SkeletonPosingCapability>(out var skeletonPosingCapability)) {
            ImCallback.TreeNode("Skeleton Posing", () => {
                ImGui.Text($"Active Body Timelines: {skeletonPosingCapability.ActiveBodyTimelines} ({timelineIdentification.GetBodyPoseName(skeletonPosingCapability.ActiveBodyTimelines.Item1, skeletonPosingCapability.ActiveBodyTimelines.Item2)})");
                ImGui.Text($"Active Face Timelines: {skeletonPosingCapability.ActiveFaceTimeline} ({timelineIdentification.GetExpressionName(skeletonPosingCapability.ActiveFaceTimeline)})");

                var chr = (Character*)actorEntity.GameObject.Address;
                var animationState = ipcService.GetAnimationState(chr);
                
                
                
                if(skeletonPosingCapability.BodyPoses.Count > 0) {
                    ImCallback.TreeNode("Body Poses", () => {
                        foreach(var (timelines, pose) in skeletonPosingCapability.BodyPoses) {
                            ImCallback.TreeNode($"Body Pose [{timelines}] ({timelineIdentification.GetBodyPoseName(timelines.Item1, timelines.Item2)})", () => {
                                foreach(var (b, sc) in pose.StackCounts) {
                                    ImGui.Text($"{b.BoneName}[{b.Slot}/{b.Partial}]: {sc}");
                                }
                            });
                        }
                    });
                }

                if(skeletonPosingCapability.FacePoses.Count > 0) {
                    ImCallback.TreeNode("Face Poses", () => {
                        foreach(var (timeline, pose) in skeletonPosingCapability.FacePoses) {
                            ImCallback.TreeNode($"Face Pose [{timeline}] ({timelineIdentification.GetExpressionName(timeline)})", () => {
                                foreach(var (b, sc) in pose.StackCounts) {
                                    ImGui.Text($"{b}: {sc}");
                                }
                            });
                        }
                    });
                }

                if(actorEntity.GameObject.ObjectIndex > 0) {
                    ImCallback.TreeNode("IPC Data", () => {
                        if(skeletonPosingCapability.IpcDataJson == null) {
                            ImGui.Text("NULL");
                            return;
                        }

                        ImGui.TextWrapped(skeletonPosingCapability.IpcDataJson);
                    });
                }
            });
        }
    }

    public override void Draw() {
        ImCallback.TabBar("LivePoseDebug", () => {
            ImCallback.TabItem("Characters", () => {
                if(!LivePose.TryGetService<EntityManager>(out var entityManager)) {
                    ImGui.TextColored(ImGuiColors.DalamudRed, "Failed to get EntityManager service.");
                    return;
                }

                foreach(var e in entityManager.TryGetAllActors()) {
                    using(ImRaii.PushId($"entity_{e.Id.Unique}")) {
                        ImCallback.TreeNode(e.FriendlyName, () => { ShowEntity(e); });
                    }
                }
            });
            
            ImCallback.TabItem("Timelines", () => {
                if(!LivePose.TryGetService<IDataManager>(out var dataManager)) {
                    ImGui.TextColored(ImGuiColors.DalamudRed, "Failed to get DataManager service.");
                    return;
                }

                
                ImCallback.TreeNode("Face Expressions", () => {
                    foreach(var emote in dataManager.GetExcelSheet<Emote>()) {
                        if (emote.EmoteCategory.RowId != 3) continue;
                        
                        
                        ImGui.Text($"{emote.Name.ExtractText()}: {emote.ActionTimeline[0].RowId}");
                        
                        
                        
                    }
                });
                
                ImCallback.TreeNode("Emotes", () => {
                    foreach(var emote in dataManager.GetExcelSheet<Emote>()) {
                        if (string.IsNullOrWhiteSpace(emote.Name.ExtractText())) continue;
                        
                        ImGui.Text($"{emote.Name.ExtractText()} ({emote.EmoteCategory.Value.Name})");
                        using(ImRaii.PushIndent()) {
                            foreach(var tl in emote.ActionTimeline) {
                                if (!tl.IsValid) continue;
                                if (tl.RowId == 0) continue;


                                switch(tl.Value.Slot) {
                                    case 0:
                                        ImGui.Text($"Timeline {tl.RowId} in Main slot");
                                        break;
                                    case 1:
                                        ImGui.Text($"Timeline {tl.RowId} in UpperBody slot");
                                        break;
                                }
                    
                    
                    
                    
                    
                            }
                        }
                        
                    }
                });
                
                
                
                
                






            });
            
            
            
        });
    }
}
