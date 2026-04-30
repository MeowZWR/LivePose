using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace LivePose.UI;

public static class ImCallback {
    public static void TreeNode(ImU8String label, Action callback, Action? afterDraw = null) {
        using var node = ImRaii.TreeNode(label);
        afterDraw?.Invoke();
        if(node.Success) callback();
    }
    
    public static void TabBar(ImU8String label, Action callback, Action? afterDraw = null)  {
        using var node = ImRaii.TabBar(label);
        afterDraw?.Invoke();
        if(node.Success) callback();
    }
    
    public static void TabItem(ImU8String label, Action callback, Action? afterDraw = null)  {
        using var node = ImRaii.TabItem(label);
        afterDraw?.Invoke();
        if(node.Success) callback();
    }

}
