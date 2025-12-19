using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using LivePose.UI;
using System;
using LivePose.Capabilities.Posing;
using LivePose.Entities;
using LivePose.Entities.Core;

namespace LivePose.Game.Chat;

public class CommandHandlerService : IDisposable
{
    private const string LivePoseCommandName = "/livepose";

    private readonly ICommandManager _commandManager;
    private readonly IChatGui _chatGui;
    private readonly UIManager _uiManager;
    private readonly IObjectTable _objectTable;
    

    public CommandHandlerService(ICommandManager commandManager, IChatGui chatGui, UIManager uiManager, IObjectTable objectTable)
    {
        _commandManager = commandManager;
        _chatGui = chatGui;
        _uiManager = uiManager;
        _objectTable = objectTable;

        _commandManager.AddHandler(LivePoseCommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggles the LivePose overlay.",
            ShowInHelp = true,
        });
    }

    private void OnCommand(string command, string arguments)
    {
        if(arguments.Length == 0)
            arguments = "overlay";

        var argumentList = arguments.Split(' ', 2);

        switch(argumentList[0].ToLowerInvariant())
        {
            case "overlay":
                if(_objectTable.LocalPlayer != null) {
                    if(LivePose.TryGetService<EntityManager>(out var manager)) {
                        if(manager.TryGetEntity(new EntityId(_objectTable.LocalPlayer), out var entity)) {
                            if(entity.TryGetCapability<PosingCapability>(out var posingCapability)) {
                                manager.SetSelectedEntity(entity);
                                posingCapability.OverlayOpen = true;
                            }
                        }
                        
                    }
                }
                
                break;

            case "settings":
                _uiManager.ToggleSettingsWindow();
                break;

            case "help":
            default:
                PrintHelp();
                break;
        }

    }

    private void PrintHelp()
    {
        _chatGui.Print("Valid Brio Commands Are:");
        _chatGui.Print("<none> - Toggle main Brio window");
        _chatGui.Print("window - Toggle main Brio window");
        _chatGui.Print("settings - Toggle Brio settings window");
        _chatGui.Print("about - Toggle Brio info window");
        _chatGui.Print("help - Print this help prompt");
    }

    public void Dispose()
    {
        _commandManager.RemoveHandler(LivePoseCommandName);
    }
}
