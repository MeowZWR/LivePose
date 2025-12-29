using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using LivePose.Capabilities.Actor;
using LivePose.Entities;
using System;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Lumina.Excel.Sheets;
using Companion = FFXIVClientStructs.FFXIV.Client.Game.Character.Companion;

namespace LivePose.Game.Actor;

public unsafe class ActionTimelineService : IDisposable
{
    public enum ActionTimelineSlots : int
    {
        Base = 0,
        UpperBody = 1,
        Facial = 2,
        Add = 3,
        // 4-6 unknown purpose
        Lips = 7,
        Parts1 = 8,
        Parts2 = 9,
        Parts3 = 10,
        Parts4 = 11,
        Overlay = 12
    }

    private delegate void CalculateAndApplyOverallSpeedDelegate(TimelineContainer* a1);
    private readonly Hook<CalculateAndApplyOverallSpeedDelegate> _calculateAndApplyOverallSpeedHook = null!;

    private readonly EntityManager _entityManager;

    public ActionTimelineService(EntityManager entityManager, ISigScanner scanner, IGameInteropProvider hooking)
    {
        _entityManager = entityManager;

        var calculateAndApplyAddress = scanner.ScanText("E8 ?? ?? ?? ?? 48 8D 8B ?? ?? ?? ?? 48 8B 01 FF 50 ?? 48 8D 8B ?? ?? ?? ?? 48 8B 01 FF 50 ?? F6 83");
        _calculateAndApplyOverallSpeedHook = hooking.HookFromAddress<CalculateAndApplyOverallSpeedDelegate>(calculateAndApplyAddress, CalculateAndApplyOverallSpeedDetour);
        _calculateAndApplyOverallSpeedHook.Enable();
    }


    private static readonly HashSet<uint> freezeableTimelines = [
        3, 3124, 3126, 3182, 3184, 7405, 7407,  // Idle Poses
    ];

    static ActionTimelineService() {
        if(!LivePose.TryGetService<IDataManager>(out var dataManager)) {
            LivePose.Log.Error("Failed to get DataManager.");
        }

        foreach(var e in dataManager.GetExcelSheet<Emote>()) {
            foreach(var t in e.ActionTimeline) {
                if(t is { RowId: > 0, IsValid: true }) {
                    if(t.Value.IsLoop) {
                        freezeableTimelines.Add(t.RowId);
                    }
                }
            }
        }
        
        
        
    }
    

    private void CalculateAndApplyOverallSpeedDetour(TimelineContainer* a1)
    {
        _calculateAndApplyOverallSpeedHook.Original(a1);
        if(a1->OwnerObject->ObjectKind == ObjectKind.Companion) {
            var companion = (Companion*)a1->OwnerObject;
            var owner = companion->Owner;
            if(owner == null) return;
            if(!_entityManager.TryGetEntity(&owner->Character, out var ownerEntity)) return;
            if(!ownerEntity.TryGetCapability<ActionTimelineCapability>(out var ownerAct)) return;
            if(!ownerAct.MinionSpeedMultiplierOverride.HasValue) return;
            a1->OverallSpeed = ownerAct.MinionSpeedMultiplierOverride.Value;
            return;
        }
        
        if(!freezeableTimelines.Contains(a1->TimelineSequencer.TimelineIds[0])) return;
        if(!_entityManager.TryGetEntity(a1->OwnerObject, out var entity)) return;
        if(!entity.TryGetCapability<ActionTimelineCapability>(out var atc)) return;
        if(!atc.SpeedMultiplierOverride.HasValue) return;
        a1->OverallSpeed = atc.SpeedMultiplierOverride.Value;
    }

    public void Dispose()
    {
        _calculateAndApplyOverallSpeedHook.Dispose();
    }
}
