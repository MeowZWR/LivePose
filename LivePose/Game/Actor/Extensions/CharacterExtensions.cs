namespace LivePose.Game.Actor.Extensions;

using Dalamud.Game.ClientState.Objects.Types;
using global::LivePose.Game.Actor.Appearance;
using global::LivePose.Game.Actor.Interop;
using global::LivePose.Game.Posing;
using global::LivePose.Game.Types;
using System;
using System.Collections.Generic;
using StructsDrawObjectData = FFXIVClientStructs.FFXIV.Client.Game.Character.DrawObjectData;
using StructsBattleCharacter = FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara;
using StructsCharacter = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using StructsCharacterBase = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.CharacterBase;
using StructsDrawDataContainer = FFXIVClientStructs.FFXIV.Client.Game.Character.DrawDataContainer;

public static class CharacterExtensions
{
    public static unsafe StructsCharacter* Native(this ICharacter go)
    {
        return (StructsCharacter*)go.Address;
    }

    public static unsafe bool HasCompanionSlot(this ICharacter go)
    {
        var native = go.Native();
        return native->CompanionObject != null;
    }

    public static unsafe bool HasSpawnedCompanion(this ICharacter go)
    {
        var native = go.Native();
        return native->CompanionObject != null &&
            (
            native->OrnamentData.OrnamentObject != null ||
            native->Mount.MountObject != null ||
            native->CompanionData.CompanionObject != null
            );
    }

    public static unsafe StructsBattleCharacter* Native(this IBattleChara go)
    {
        return (StructsBattleCharacter*)go.Address;
    }

    public static unsafe StructsDrawObjectData* GetWeaponDrawObjectData(this ICharacter go, ActorEquipSlot slot)
    {
        StructsDrawDataContainer.WeaponSlot? weaponSlot = slot switch
        {
            ActorEquipSlot.MainHand => StructsDrawDataContainer.WeaponSlot.MainHand,
            ActorEquipSlot.OffHand => StructsDrawDataContainer.WeaponSlot.OffHand,
            ActorEquipSlot.Prop => StructsDrawDataContainer.WeaponSlot.Unk,
            _ => throw new Exception("Invalid weapon slot")
        };

        if(!weaponSlot.HasValue)
            return null;

        var drawData = &go.Native()->DrawData;

        fixed(StructsDrawObjectData* drawObjData = &drawData->Weapon(weaponSlot.Value))
        {
            StructsDrawObjectData* drawObjectData = drawObjData;
            return drawObjectData;
        }
    }

    public static unsafe StructsDrawObjectData* GetOrnamentDrawObjectData(this ICharacter go) {
        var chr = (StructsCharacter*)go.Address;
        if(chr == null) return null;


        if(chr->OrnamentData.OrnamentObject == null) return null;

        var drawData = (StructsDrawObjectData*) &chr->OrnamentData.OrnamentObject->DrawData;
        return drawData;
    }

    public static unsafe BrioCharacterBase* GetCharacterBase(this ICharacter go) => go.GetDrawObject<BrioCharacterBase>();

    public unsafe class CharacterBaseInfo
    {
        public BrioCharacterBase* CharacterBase;
        public PoseInfoSlot Slot;
    }

    public static unsafe IReadOnlyList<CharacterBaseInfo> GetCharacterBases(this ICharacter go)
    {
        var list = new List<CharacterBaseInfo>();
        var charaBase = go.GetCharacterBase();

        if(charaBase != null)
            list.Add(new CharacterBaseInfo { CharacterBase = charaBase, Slot = PoseInfoSlot.Character });

        charaBase = go.GetWeaponCharacterBase(ActorEquipSlot.MainHand);
        if(charaBase != null)
            list.Add(new CharacterBaseInfo { CharacterBase = charaBase, Slot = PoseInfoSlot.MainHand });

        charaBase = go.GetWeaponCharacterBase(ActorEquipSlot.OffHand);
        if(charaBase != null)
            list.Add(new CharacterBaseInfo { CharacterBase = charaBase, Slot = PoseInfoSlot.OffHand });

        charaBase = go.GetWeaponCharacterBase(ActorEquipSlot.Prop);
        if(charaBase != null)
            list.Add(new CharacterBaseInfo { CharacterBase = charaBase, Slot = PoseInfoSlot.Prop });

        charaBase = go.GetOrnamentBase();
        if(charaBase != null) {
            list.Add(new CharacterBaseInfo() { CharacterBase = charaBase, Slot = PoseInfoSlot.Ornament });
        }

        return list;
    }

    public static unsafe BrioCharacterBase* GetWeaponCharacterBase(this ICharacter go, ActorEquipSlot slot)
    {
        var weaponDrawData = go.GetWeaponDrawObjectData(slot);
        if(weaponDrawData != null)
        {
            return (BrioCharacterBase*)weaponDrawData->DrawObject;
        }

        return null;
    }

    public static unsafe BrioCharacterBase* GetOrnamentBase(this ICharacter go) {
        var ornament = go.Native()->OrnamentData.OrnamentObject;
        if(ornament == null) return null;
        return (BrioCharacterBase*)ornament->DrawObject;
    }

    public static unsafe BrioHuman* GetHuman(this ICharacter go)
    {
        var charaBase = go.GetCharacterBase();
        if(charaBase == null)
            return null;

        if(charaBase->CharacterBase.GetModelType() != StructsCharacterBase.ModelType.Human)
            return null;

        return (BrioHuman*)charaBase;
    }

    public static unsafe BrioHuman.ShaderParams* GetShaderParams(this ICharacter go)
    {
        var human = go.GetHuman();
        if(human != null && human->Shaders != null && human->Shaders->Params != null)
        {

            return human->Shaders->Params;
        }
        return null;
    }
}
