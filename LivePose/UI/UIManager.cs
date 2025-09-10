using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using LivePose.Config;
using LivePose.Game.GPose;
using LivePose.IPC;
using LivePose.UI.Controls;
using LivePose.UI.Windows;
using LivePose.UI.Windows.Specialized;
using System;
using System.Collections.Generic;

namespace LivePose.UI;

public class UIManager : IDisposable
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly GPoseService _gPoseService;
    private readonly ConfigurationService _configurationService;
    
    private readonly SettingsWindow _settingsWindow;
    private readonly LibraryWindow _libraryWindow;
    private readonly PosingOverlayWindow _overlayWindow;
    private readonly PosingOverlayToolbarWindow _overlayToolbarWindow;
    private readonly PosingTransformWindow _overlayTransformWindow;
    private readonly PosingGraphicalWindow _graphicalWindow;

    private readonly ITextureProvider _textureProvider;
    private readonly IToastGui _toastGui;

    private readonly WindowSystem _windowSystem;

    public readonly FileDialogManager FileDialogManager = new()
    {
        AddedWindowFlags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking
    };

    private readonly List<Window> _hiddenWindows = [];

    public ITextureProvider TextureProvider => _textureProvider;

    public static UIManager Instance { get; private set; } = null!;

    public static bool IsPosingGraphicalWindowOpen => Instance._graphicalWindow.IsOpen;
    
    public bool IsActorPoseWindowOpen => _graphicalWindow.IsOpen;

    public UIManager
        (
            IDalamudPluginInterface pluginInterface,
            GPoseService gPoseService,
            ConfigurationService configurationService,
            ITextureProvider textureProvider,
            IToastGui toast,
            SettingsWindow settingsWindow,
            LibraryWindow libraryWindow,
            PosingOverlayWindow overlayWindow,
            PosingOverlayToolbarWindow overlayToolbarWindow,
            PosingTransformWindow overlayTransformWindow,
            PosingGraphicalWindow graphicalWindow
        )
    {
        Instance = this;

        _pluginInterface = pluginInterface;
        _gPoseService = gPoseService;
        _configurationService = configurationService;
        _textureProvider = textureProvider;
        _toastGui = toast;
        
        _settingsWindow = settingsWindow;
        _libraryWindow = libraryWindow;
        _overlayWindow = overlayWindow;
        _overlayToolbarWindow = overlayToolbarWindow;
        _overlayTransformWindow = overlayTransformWindow;
        _graphicalWindow = graphicalWindow;

        _windowSystem = new(LivePose.Name);
        
        _windowSystem.AddWindow(_settingsWindow);
        _windowSystem.AddWindow(_libraryWindow);
        _windowSystem.AddWindow(_overlayWindow);
        _windowSystem.AddWindow(_overlayToolbarWindow);
        _windowSystem.AddWindow(_overlayTransformWindow);
        _windowSystem.AddWindow(_graphicalWindow);

        _pluginInterface.UiBuilder.Draw += DrawUI;

        if(LivePose.IsPlugin) {
            _pluginInterface.UiBuilder.OpenConfigUi += ShowSettingsWindow;
        }
    }

    public void ToggleGraphicalPosingWindow()
    {
        _graphicalWindow.IsOpen = !_graphicalWindow.IsOpen;
    }

    public void ShowSettingsWindow()
    {
        _settingsWindow.IsOpen = true;
    }
    
    public void NotifyError(string message)
    {
        _toastGui.ShowError(message);
    }
    
    public void ToggleSettingsWindow() => _settingsWindow.IsOpen = !_settingsWindow.IsOpen;
    
    private void DrawUI() {
        _windowSystem.Draw();
        FileDialogManager.Draw();
        _libraryWindow.DrawModal();
        RenameActorModal.DrawModal();
    }
    
    public void TemporarilyHideAllOpenWindows()
    {
        foreach(var window in _windowSystem.Windows)
        {
            if(window.IsOpen == true)
            {
                _hiddenWindows.Add(window);
                window.IsOpen = false;
            }
        }
    }

    public void ReopenAllTemporarilyHiddenWindows()
    {
        foreach(var window in _hiddenWindows)
        {
            window.IsOpen = true;
        }
        _hiddenWindows.Clear();
    }

    public void Dispose()
    {
        _pluginInterface.UiBuilder.Draw -= DrawUI;
        _pluginInterface.UiBuilder.OpenConfigUi -= ShowSettingsWindow;

        _windowSystem.RemoveAllWindows();

        Instance = null!;

        GC.SuppressFinalize(this);
    }

    public IDalamudTextureWrap LoadImage(byte[] data)
    {
        var imgTask = _textureProvider.CreateFromImageAsync(data);
        imgTask.Wait(); // TODO: Don't block
        var img = imgTask.Result;
        return img;
    }
}
