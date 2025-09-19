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
    private readonly IClientState _clientState;

    public CommandHandlerService(ICommandManager commandManager, IChatGui chatGui, UIManager uiManager, IClientState clientState)
    {
        _commandManager = commandManager;
        _chatGui = chatGui;
        _uiManager = uiManager;
        _clientState = clientState;

        _commandManager.AddHandler(LivePoseCommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "切换 LivePose 叠加层显示。",
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
                if(_clientState.LocalPlayer != null) {
                    if(LivePose.TryGetService<EntityManager>(out var manager)) {
                        if(manager.TryGetEntity(new EntityId(_clientState.LocalPlayer), out var entity)) {
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
        _chatGui.Print("可用的 LivePose 指令有：");
        _chatGui.Print("<无> - 切换主 LivePose 窗口");
        _chatGui.Print("window - 切换主 LivePose 窗口");
        _chatGui.Print("settings - 切换 LivePose 设置窗口");
        _chatGui.Print("about - 切换 LivePose 信息窗口");
        _chatGui.Print("help - 显示此帮助提示");
    }

    public void Dispose()
    {
        _commandManager.RemoveHandler(LivePoseCommandName);
    }
}
