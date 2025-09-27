using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;

namespace LivePose.Core;

public static class Extensions {
    public static bool AnyUnsafe(this ICondition condition) {
        return condition.Any(ConditionFlag.BetweenAreas, ConditionFlag.CreatingCharacter, ConditionFlag.LoggingOut,
            ConditionFlag.BetweenAreas51, ConditionFlag.Transformed, ConditionFlag.WatchingCutscene, ConditionFlag.WatchingCutscene78,
            ConditionFlag.OccupiedInCutSceneEvent, ConditionFlag.BeingMoved, ConditionFlag.EditingPortrait, ConditionFlag.CarryingItem,
            ConditionFlag.CarryingObject, ConditionFlag.Disguised, ConditionFlag.Crafting, ConditionFlag.ExecutingCraftingAction,
            ConditionFlag.Gathering, ConditionFlag.ExecutingGatheringAction);
    }
        
}
