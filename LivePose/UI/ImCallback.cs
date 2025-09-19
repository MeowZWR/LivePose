using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace LivePose.UI;

public static class ImCallback {
    public static void TreeNode(ImU8String label, Action callback, Action? afterDraw = null) => ImRaii.TreeNode(label).Callback(callback, afterDraw);
    public static void TreeNode(ImU8String label, ImGuiTreeNodeFlags flags, Action callback, Action? afterDraw = null) => ImRaii.TreeNode(label, flags).Callback(callback, afterDraw);
    public static void TabBar(ImU8String label, Action callback, Action? afterDraw = null) => ImRaii.TabBar(label).Callback(callback, afterDraw);
    public static void TabBar(ImU8String label, ImGuiTabBarFlags flags, Action callback, Action? afterDraw = null) => ImRaii.TabBar(label, flags).Callback(callback, afterDraw);
    public static void TabItem(ImU8String label, Action callback, Action? afterDraw = null) => ImRaii.TabItem(label).Callback(callback, afterDraw);
    public static void TabItem(ImU8String label, ImGuiTabItemFlags flags, Action callback, Action? afterDraw = null) => ImRaii.TabItem(label, flags).Callback(callback, afterDraw);
    public static void TabItem(ImU8String label, ref bool open, Action callback, Action? afterDraw = null) => ImRaii.TabItem(label, ref open).Callback(callback, afterDraw);
    public static void TabItem(ImU8String label, ref bool open, ImGuiTabItemFlags flags, Action callback, Action? afterDraw = null) => ImRaii.TabItem(label, ref open, flags).Callback(callback, afterDraw);

    private static void Callback<T>(this T endObj, Action callback, Action? afterDraw) where T : ImRaii.IEndObject {
        try {
            afterDraw?.Invoke();
            if(endObj.Success) callback();
        } finally {
            endObj.Dispose();
        }
    }

}
