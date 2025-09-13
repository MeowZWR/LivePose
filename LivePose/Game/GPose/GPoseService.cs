using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using LivePose.Config;
using System;
using System.IO;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using LivePose.Capabilities.Posing;
using LivePose.Entities;
using LivePose.Entities.Core;
using LivePose.IPC;
using Newtonsoft.Json;
using Swan;
using JsonSerializer = LivePose.Core.JsonSerializer;
using NativeCharacter = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace LivePose.Game.GPose;

public unsafe class GPoseService : IDisposable
{
    public bool IsGPosing => _isInFakeGPose || _isInGPose;

    public delegate void OnGPoseStateDelegate(bool newState);
    public event OnGPoseStateDelegate? OnGPoseStateChange;

    public bool FakeGPose
    {
        get => _isInFakeGPose;
        set
        {
            if(_isInFakeGPose == value)
                return;

            _isInFakeGPose = value;

            TriggerGPoseChange();
        }
    }

    private bool _isInGPose = false;
    private bool _isInFakeGPose = false;

    private delegate bool GPoseEnterExitDelegate(UIModule* uiModule);
    private delegate void ExitGPoseDelegate(UIModule* uiModule);
    private readonly Hook<GPoseEnterExitDelegate> _enterGPoseHook;
    private readonly Hook<ExitGPoseDelegate> _exitGPoseHook;
    
    private readonly Hook<CharacterSetupContainer.Delegates.CopyFromCharacter> _copyFromCharacterHook;

    private readonly IFramework _framework;
    private readonly IClientState _clientState;
    private readonly ConfigurationService _configService;
    private readonly EntityManager _entityManager;
    private readonly IObjectTable _objectTable;
    private readonly BrioService _brioService;
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly IGameGui _gameGui;

    public const string BrioHiddenName = "[HIDDEN]";

    public GPoseService(IFramework framework, IClientState clientState, ConfigurationService configService, IGameInteropProvider interopProvider, ISigScanner scanner, EntityManager entityManager, IObjectTable objectTable, BrioService brioService, IDalamudPluginInterface pluginInterface, IGameGui gameGui)
    {
        _framework = framework;
        _clientState = clientState;
        _configService = configService;
        _entityManager = entityManager;
        _objectTable = objectTable;
        _brioService = brioService;
        _pluginInterface = pluginInterface;
        _gameGui = gameGui;

        _isInGPose = _clientState.IsGPosing;

        UIModule* uiModule = Framework.Instance()->UIModule;
        var enterGPoseAddress = (nint)uiModule->VirtualTable->EnterGPose;
        var exitGPoseAddress = (nint)uiModule->VirtualTable->ExitGPose;

        _enterGPoseHook = interopProvider.HookFromAddress<GPoseEnterExitDelegate>(enterGPoseAddress, EnteringGPoseDetour);
        _enterGPoseHook.Enable();

        _exitGPoseHook = interopProvider.HookFromAddress<ExitGPoseDelegate>(exitGPoseAddress, ExitingGPoseDetour);
        _exitGPoseHook.Enable();

        _copyFromCharacterHook = interopProvider.HookFromAddress<CharacterSetupContainer.Delegates.CopyFromCharacter>(CharacterSetupContainer.Addresses.CopyFromCharacter.Value, CopyFromCharacterDetour);
        _copyFromCharacterHook.Enable();

        _framework.Update += OnFrameworkUpdate;
    }

    private ulong CopyFromCharacterDetour(CharacterSetupContainer* thisPtr, Character* source, CharacterSetupContainer.CopyFlags flags) {
        try {
            return _copyFromCharacterHook.Original(thisPtr, source, flags);
        } finally {
            try {
                OnCopyActor(source, thisPtr->OwnerObject);
            } catch(Exception ex) {
                LivePose.Log.Error(ex, "Error handling OnCopyActor");
            }
        }
    }

    private void OnCopyActor(Character* source, Character* destination) {
        if(source == null || source->ObjectIndex >= 200) return;
        if(destination == null || source->ObjectIndex < 200 || source->ObjectIndex > 439) return;
        
        var obj = _objectTable.CreateObjectReference((nint)source);
        if(obj is not IPlayerCharacter sourceCharacter) return;

        var destObj = _objectTable.CreateObjectReference((nint)destination);
        if(destObj == null) return;
        
        LivePose.Log.Verbose($"Copy Character: {sourceCharacter.Name} -> {destination->ObjectIndex}");

        if(!_entityManager.TryGetEntity(new EntityId(sourceCharacter), out var entity)) return;
        if(!entity.TryGetCapability<PosingCapability>(out var posing)) return;
        var pose = posing.ExportPose();

        var json = JsonSerializer.Serialize(pose);
        
        TrySetPose(destObj, json);
    }

    private void TrySetPose(IGameObject obj, string json, int total = 0, int success = 0) {
        if(success > 2) return;
        if(total > 100) return;
        _framework.RunOnTick(() => {
            var fadeAddon = _gameGui.GetAddonByName("FadeMiddle");
            if(fadeAddon == null || !fadeAddon.IsVisible) {
                LivePose.Log.Warning("Send Pose to Brio");
                var s = _brioService.SetPose(obj, json);
                TrySetPose(obj, json, total++, success = s ? success + 1 : success);
            } else {
                TrySetPose(obj, json, total++, success);
            }
            
        }, delayTicks: 5);
    }
    

    public void TriggerGPoseChange()
    {
        var gposing = IsGPosing;
        LivePose.Log.Debug($"GPose state changed to {gposing}");
        OnGPoseStateChange?.Invoke(gposing);
    }

    public void AddCharacterToGPose(ICharacter chara) => AddCharacterToGPose((NativeCharacter*)chara.Address);

    public void AddCharacterToGPose(NativeCharacter* chara)
    {
        if(!IsGPosing)
            return;

        var ef = EventFramework.Instance();
        if(ef == null)
            return;

        ef->EventSceneModule.EventGPoseController.AddCharacterToGPose(chara);
    }

    private void ExitingGPoseDetour(UIModule* uiModule)
    {
        _exitGPoseHook.Original.Invoke(uiModule);
        HandleGPoseStateChange(false);
    }

    private bool EnteringGPoseDetour(UIModule* uiModule)
    {
        bool didEnter = _enterGPoseHook.Original.Invoke(uiModule);

        HandleGPoseStateChange(didEnter);

        return didEnter;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        // Only detect if we got snapped out
        if(!_clientState.IsGPosing && _isInGPose)
            HandleGPoseStateChange(_clientState.IsGPosing);
    }

    private void HandleGPoseStateChange(bool newState)
    {
        if(IsGPosing == newState || _isInFakeGPose)
            return;

        _isInGPose = newState;

        TriggerGPoseChange();
    }

    public void Dispose()
    {
        _framework.Update -= OnFrameworkUpdate;
        
        _enterGPoseHook.Dispose();
        _exitGPoseHook.Dispose();
        _copyFromCharacterHook.Dispose();

        GC.SuppressFinalize(this);
    }
}

