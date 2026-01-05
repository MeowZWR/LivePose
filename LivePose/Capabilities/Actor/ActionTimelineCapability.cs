using LivePose.Game.Actor.Extensions;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using LivePose.Entities.Actor;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Numerics;
using LivePose.Core;
using LivePose.IPC;

namespace LivePose.Capabilities.Actor;

public class ActionTimelineCapability(IFramework framework, ActorEntity parent) : ActorCharacterCapability(parent) {
    public unsafe float SpeedMultiplier => SpeedMultiplierOverride ?? Character.Native()->Timeline.OverallSpeed;

    public unsafe float MinionSpeedMultiplier {
        get {
            if(MinionSpeedMultiplierOverride != null) return MinionSpeedMultiplierOverride.Value;
            var chr = Character.GetMinion();
            if(chr == null) return 1;
            return chr->Timeline.OverallSpeed;
        }
    }
    
    public Vector3? SpeedMultiplierPosition { get; private set; }

    public float? SpeedMultiplierOverride {
        get {
            if(field == null) return null;
            
            if(SpeedMultiplierPosition == null || Vector3.DistanceSquared(SpeedMultiplierPosition.Value, Character.Position) > 0.01f) {
                ResetOverallSpeedOverride();
                if(GameObject.ObjectIndex == 0) {
                    if(LivePose.TryGetService<HeelsService>(out var service) && service.IsAvailable) {
                        service.SetPlayerPoseTag();
                    }
                }
                return null;
            }

            return field.Value.IsApproximatelySame(1) ? null : field;
        }

        private set;
    }
    
    public float? MinionSpeedMultiplierOverride {
        get => field == null || field.Value.IsApproximatelySame(1) ? null : field;
        private set;
    }
    

    public unsafe void SetOverallSpeedOverride(float speed) {
        if(speed.IsApproximatelySame(1)) {
            ResetOverallSpeedOverride();
            return;
        }

        speed = Math.Clamp(speed, -2, 2);
        SpeedMultiplierPosition = Character.Position;
        SpeedMultiplierOverride = speed;
        Character.Native()->Timeline.OverallSpeed = speed;
    }

    public unsafe void SetMinionSpeedOverride(float speed) {
        if(speed.IsApproximatelySame(1)) {
            ResetMinionSpeedOverride();
            return;
        }

        var chr = Character.GetMinion();
        if(chr == null) return;
        
        speed = Math.Clamp(speed, -2, 2);
        MinionSpeedMultiplierOverride = speed;
        chr->Timeline.OverallSpeed = speed;
    }

    public void ResetOverallSpeedOverride() {
        SpeedMultiplierPosition = null;
        SpeedMultiplierOverride = null;
    }

    public void ResetMinionSpeedOverride() {
        MinionSpeedMultiplierOverride = null;
    }


    private unsafe void ResetTimeline(DrawObject* drawObj) {
        if(drawObj == null) return;

        if(drawObj->Object.GetObjectType() != ObjectType.CharacterBase)
            return;

        var charaBase = (CharacterBase*)drawObj;
        if(charaBase->Skeleton == null)
            return;

        var skeleton = charaBase->Skeleton;
        for(int p = 0; p < skeleton->PartialSkeletonCount; ++p) {
            var partial = &skeleton->PartialSkeletons[p];

            var animatedSkele = partial->GetHavokAnimatedSkeleton(0);
            if(animatedSkele == null)
                continue;

            for(int c = 0; c < animatedSkele->AnimationControls.Length; ++c) {
                var control = animatedSkele->AnimationControls[c].Value;
                if(control == null)
                    continue;

                var binding = control->hkaAnimationControl.Binding;
                if(binding.ptr == null)
                    continue;

                var anim = binding.ptr->Animation.ptr;
                if(anim == null)
                    continue;

                if(control->PlaybackSpeed == 0) {
                    control->hkaAnimationControl.LocalTime = 0;
                    LivePose.Log.Verbose($"hkaAnimationControl");
                }
            }
        }
    }
    
    public async void StopSpeedAndResetTimeline(Action? postStopAction = null, bool resetSpeedAfterAction = false) {
        LivePose.Log.Verbose($"StopSpeedAndResetTimeline {postStopAction is not null} {resetSpeedAfterAction}");

        var oldSpeed = SpeedMultiplier;
        var oldMinionSpeed = MinionSpeedMultiplier;

        SetOverallSpeedOverride(0);
        SetMinionSpeedOverride(0);

        LivePose.Log.Verbose($"SetOverallSpeedOverride {oldSpeed} {SpeedMultiplier}");

        await framework.RunOnTick(() => {
            unsafe {
                ResetTimeline(Character.Native()->DrawObject);
                var minion = Character.GetMinionBase();
                if(minion != null) ResetTimeline((DrawObject*)minion);
            }
        }, delayTicks: 4);

        postStopAction?.Invoke();

        LivePose.Log.Verbose($"postStopAction Invoke: {postStopAction is not null}");

        if(resetSpeedAfterAction) {
            await framework.RunOnTick(() => {
                SetOverallSpeedOverride(oldSpeed);
                SetMinionSpeedOverride(oldMinionSpeed);

                LivePose.Log.Verbose($"SetOverallSpeedOverride {SpeedMultiplier}");
            }, delayTicks: 2);
        }
    }

    public override void Dispose() {
        ResetOverallSpeedOverride();
        ResetMinionSpeedOverride();
        base.Dispose();
    }

    public static ActionTimelineCapability? CreateIfEligible(IServiceProvider provider, ActorEntity entity) {
        return entity.GameObject is ICharacter ? ActivatorUtilities.CreateInstance<ActionTimelineCapability>(provider, entity) : null;
    }
}
