using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using LivePose.Capabilities.Posing;
using LivePose.Entities;
using LivePose.Entities.Actor;

namespace LivePose.UI.Windows;

public class DebugWindow() : Window($"{LivePose.Name} Debug") {
    private void ShowEntity(ActorEntity actorEntity) {
        if(actorEntity.TryGetCapability<SkeletonPosingCapability>(out var skeletonPosingCapability)) {
            ImCallback.TreeNode("Skeleton Posing", () => {
                ImGui.Text($"Active Body Timelines: {skeletonPosingCapability.ActiveBodyTimelines}");
                ImGui.Text($"Active Face Timelines: {skeletonPosingCapability.ActiveFaceTimeline}");

                if(skeletonPosingCapability.BodyPoses.Count > 0) {
                    ImCallback.TreeNode("Body Poses", () => {
                        foreach(var (timelines, pose) in skeletonPosingCapability.BodyPoses) {
                            ImCallback.TreeNode($"Body Pose [{timelines}]", () => {
                                foreach(var (b, sc) in pose.StackCounts) {
                                    ImGui.Text($"{b}: {sc}");
                                }
                            });
                        }
                    });
                }

                if(skeletonPosingCapability.FacePoses.Count > 0) {
                    ImCallback.TreeNode("Face Poses", () => {
                        foreach(var (timeline, pose) in skeletonPosingCapability.FacePoses) {
                            ImCallback.TreeNode($"Face Pose [{timeline}]", () => {
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
        });
    }
}
