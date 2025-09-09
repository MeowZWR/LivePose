using Dalamud.Game;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using LivePose.Game.GPose;
using System;

namespace LivePose.Game.Input;

public class GameInputService : IDisposable
{
    private readonly GPoseService _gPoseService;
    
    public bool HandleAllKeys { get; set; } = false;
    public bool HandleAllMouse { get; set; } = false;

    private unsafe delegate void HandleInputDelegate(IntPtr arg1, IntPtr arg2, IntPtr arg3, MouseFrame* mouseState, KeyboardFrame* keyboardState);
    private readonly Hook<HandleInputDelegate> _handleInputHook = null!;

    public unsafe GameInputService(GPoseService gPoseService, ISigScanner scanner, IGameInteropProvider hooking)
    {
        _gPoseService = gPoseService;

        var inputHandleSig = "E8 ?? ?? ?? ?? ?? 8B ?? ?? ?? ?? 8B 87 ?? ?? ?? ?? 89 45";
        _handleInputHook = hooking.HookFromAddress<HandleInputDelegate>(scanner.ScanText(inputHandleSig), HandleInputDetour);
        _handleInputHook.Enable();
    }

    public unsafe void HandleInputDetour(IntPtr arg1, IntPtr arg2, IntPtr arg3, MouseFrame* mouseFrame, KeyboardFrame* keyboardFrame)
    {
        _handleInputHook.Original(arg1, arg2, arg3, mouseFrame, keyboardFrame);

        if(_gPoseService.IsGPosing == false)
            return;

        if(keyboardFrame->KeyState[17] == 1)
        {
            keyboardFrame->KeyState[90] = 0; // Z
            keyboardFrame->KeyState[86] = 0; // V
        }

        if(HandleAllMouse)
        {
            // TODO: Implement mouse handling logic
        }
    }

    public void Dispose()
    {
        // TODO_7_3
        _handleInputHook.Dispose();
    }
}
