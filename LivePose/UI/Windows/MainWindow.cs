using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using LivePose.Config;
using LivePose.Core;
using LivePose.Entities;
using LivePose.Game.GPose;
using LivePose.UI.Controls.Core;
using LivePose.UI.Controls.Stateless;
using LivePose.UI.Entitites;
using LivePose.UI.Theming;
using System;
using System.Numerics;

namespace LivePose.UI.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly SettingsWindow _settingsWindow;
    private readonly LibraryWindow _libraryWindow;
    private readonly ConfigurationService _configurationService;
    private readonly EntityManager _entityManager;
    private readonly EntityHierarchyView _entitySelector;
    private readonly GPoseService _gPoseService;
    private readonly HistoryService _groupedUndoService;
    private readonly IClientState _clientState;

    public MainWindow(
        ConfigurationService configService,
        SettingsWindow settingsWindow,
        LibraryWindow libraryWindow,
        EntityManager entityManager,
        HistoryService groupedUndoService,
        GPoseService gPoseService,
        IClientState clientState
        )
        : base($" {LivePose.Name} [{configService.Version}]###livepose_main_window", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize)
    {
        Namespace = "livepose_main_namespace";

        _configurationService = configService;
        _settingsWindow = settingsWindow;
        _libraryWindow = libraryWindow;
        _entityManager = entityManager;
        _gPoseService = gPoseService;
        _groupedUndoService = groupedUndoService;
        _entitySelector = new(_entityManager, _gPoseService, _groupedUndoService);
        _clientState = clientState;

        SizeConstraints = new WindowSizeConstraints
        {
            MaximumSize = new Vector2(270, 1230),
            MinimumSize = new Vector2(270, 200)
        };

        TitleBarButtons =
        [
            new()
            {
                Icon = FontAwesomeIcon.Cog,
                Click = _ => _settingsWindow.Toggle(),
                ShowTooltip = () => ImGui.SetTooltip("Settings")
            }
        ];
    }

    public override bool DrawConditions()
    {
        return base.DrawConditions() && !_clientState.IsGPosing;
    }

    public override void Draw()
    {
        DrawHeaderButtons();

        if(_gPoseService.IsGPosing == false)
        {
            using(ImRaii.PushColor(ImGuiCol.Text, UIConstants.GizmoRed))
                ImGui.Text("Open GPose to use Brio!");
        }

        var rootEntity = _entityManager.RootEntity;

        if(rootEntity is null)
            return;

        using(var container = ImRaii.Child("###entity_hierarchy_container", new Vector2(-1, ImGui.GetTextLineHeight() * 18f), true))
        {
            if(container.Success)
            {
                _entitySelector.Draw(rootEntity);

                if(_entityManager.SelectedEntityIds.Count > 1)
                {
                    using var color = ImRaii.PushColor(ImGuiCol.Text, ThemeManager.CurrentTheme.Accent.AccentColor);
                    ImGui.Text($"{_entityManager.SelectedEntityIds.Count} selected");
                }
            }
        }

        EntityHelpers.DrawEntitySection(_entityManager.SelectedEntity);
    }

    private void DrawHeaderButtons()
    {
        float buttonWidths = 25;
        float line1FinalWidth = ImBrio.GetRemainingWidth() - ((buttonWidths * 2) + (ImGui.GetStyle().ItemSpacing.X * 2) + ImGui.GetStyle().WindowBorderSize);

        float line1Width = (line1FinalWidth / 2) - 3;

        using(ImRaii.Disabled(_gPoseService.IsGPosing == false))
        {
            // This fixes a bug with text scaling
            {
                Vector2 startPos = ImGui.GetCursorPos();
                ImGui.SetCursorPos(new(-100, -100));
                ImBrio.Button("0000", FontAwesomeIcon.Bug, new Vector2(0, 0));
                ImGui.SetCursorPos(startPos);
            }
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
