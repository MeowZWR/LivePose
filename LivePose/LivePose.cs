using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using LivePose.Config;
using LivePose.Core;
using LivePose.Entities;
using LivePose.Files;
using LivePose.Game.Actor;
using LivePose.Game.Chat;
using LivePose.Game.Core;
using LivePose.Game.GPose;
using LivePose.Game.Posing;
using LivePose.IPC;
using LivePose.Library;
using LivePose.Library.Sources;
using LivePose.Resources;
using LivePose.UI;
using LivePose.UI.Controls.Stateless;
using LivePose.UI.Windows;
using LivePose.UI.Windows.Specialized;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using LivePose.Capabilities.Posing;
using LivePose.Entities.Core;

namespace LivePose;

public class LivePose : IDisposable {
    public static string Name { get; private set; } = "LivePose";

    internal static bool IsPlugin;
    
    private static ServiceProvider? _services = null;
    public static IPluginLog Log { get; private set; } = null!;
    public static IFramework Framework { get; private set; } = null!;
    
    internal LivePose(LivePosePlugin plugin, IDalamudPluginInterface pluginInterface) {
        IsPlugin = true;
        Initialize(pluginInterface);
    }

    public LivePose(IDalamudPluginInterface pluginInterface, string name = "LivePose") {
        Name = name;
        Initialize(pluginInterface);
    }
    
    private void Initialize(IDalamudPluginInterface pluginInterface) {
        
        // Setup dalamud services
        var dalamudServices = new DalamudServices(pluginInterface);
        Log = dalamudServices.Log;
        Framework = dalamudServices.Framework;

        dalamudServices.Framework.RunOnTick(() =>
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            Log.Info($"Starting {Name}...");

            try
            {
                // Setup plugin services
                var serviceCollection = SetupServices(dalamudServices);

                _services = serviceCollection.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

                // Initialize the singletons
                foreach(var service in serviceCollection)
                {
                    if(service.Lifetime == ServiceLifetime.Singleton)
                    {
                        Log.Debug($"Initializing {service.ServiceType}...");
                        _services.GetRequiredService(service.ServiceType);
                    }
                }

                // Setup default entities
                Log.Debug($"Setting up default entitites...");
                _services.GetRequiredService<EntityActorManager>().AttachContainer();
                

                // Trigger GPose events to ensure the plugin is in the correct state
                Log.Debug($"Triggering initial GPose state...");
                _services.GetRequiredService<GPoseService>().TriggerGPoseChange();

                Log.Info($"Started {Name} in {stopwatch.ElapsedMilliseconds}ms");


                _services.GetService<GPoseService>()?.FakeGPose = !dalamudServices.ClientState.IsGPosing;

            }
            catch(Exception e)
            {
                Log.Error(e, $"Failed to start {Name} in {stopwatch.ElapsedMilliseconds}ms");
                _services?.Dispose();
                throw;
            }
        }, delayTicks: 2); // TODO: Why do we need to wait several frames for some users?
    }

    private static ServiceCollection SetupServices(DalamudServices dalamudServices)
    {
        ServiceCollection serviceCollection = new();

        // Dalamud
        serviceCollection.AddSingleton(dalamudServices.PluginInterface);
        serviceCollection.AddSingleton(dalamudServices.Framework);
        serviceCollection.AddSingleton(dalamudServices.GameInteropProvider);
        serviceCollection.AddSingleton(dalamudServices.ClientState);
        serviceCollection.AddSingleton(dalamudServices.SigScanner);
        serviceCollection.AddSingleton(dalamudServices.ObjectTable);
        serviceCollection.AddSingleton(dalamudServices.DataManager);
        serviceCollection.AddSingleton(dalamudServices.CommandManager);
        serviceCollection.AddSingleton(dalamudServices.ToastGui);
        serviceCollection.AddSingleton(dalamudServices.TargetManager);
        serviceCollection.AddSingleton(dalamudServices.TextureProvider);
        serviceCollection.AddSingleton(dalamudServices.Log);
        serviceCollection.AddSingleton(dalamudServices.ChatGui);
        serviceCollection.AddSingleton(dalamudServices.Conditions);
        serviceCollection.AddSingleton(dalamudServices.GameConfig);
        serviceCollection.AddSingleton(dalamudServices.GameGui);
        
        serviceCollection.AddSingleton<FakePoseService>();
        
        // Core / Misc
        serviceCollection.AddSingleton<DalamudService>();
        serviceCollection.AddSingleton<ConfigurationService>();
        serviceCollection.AddSingleton<ResourceProvider>();
        serviceCollection.AddSingleton<GameDataProvider>();
        serviceCollection.AddSingleton<HistoryService>();

        // IPC
        serviceCollection.AddSingleton<IpcService>();
        serviceCollection.AddSingleton<HeelsService>();
        serviceCollection.AddSingleton<BrioService>();

        // Entity
        serviceCollection.AddSingleton<EntityManager>();
        serviceCollection.AddSingleton<EntityActorManager>();

        // Game
        serviceCollection.AddSingleton<ActorRedrawService>();
        serviceCollection.AddSingleton<ActionTimelineService>();
        serviceCollection.AddSingleton<GPoseService>();
        if(IsPlugin) {
            serviceCollection.AddSingleton<CommandHandlerService>();
        }

        serviceCollection.AddSingleton<SkeletonService>();
        serviceCollection.AddSingleton<PosingService>();
        serviceCollection.AddSingleton<IKService>();
        serviceCollection.AddSingleton<ObjectMonitorService>();

        // Library
        serviceCollection.AddSingleton<FileTypeInfoBase, CMToolPoseFileInfo>();
        serviceCollection.AddSingleton<FileTypeInfoBase, PoseFileInfo>();
        serviceCollection.AddSingleton<FileTypeInfoBase, LivePoseFileInfo>();
        serviceCollection.AddSingleton<FileService>();

        serviceCollection.AddSingleton<SourceBase, GameDataNpcSource>();
        serviceCollection.AddSingleton<SourceBase, GameDataMountSource>();
        serviceCollection.AddSingleton<SourceBase, GameDataOrnamentSource>();
        serviceCollection.AddSingleton<SourceBase, GameDataCompanionSource>();

        serviceCollection.AddSingleton<LibraryManager>();

        // UI
        serviceCollection.AddSingleton<UIManager>();
        serviceCollection.AddSingleton<DebugWindow>();
        serviceCollection.AddSingleton<SettingsWindow>();
        serviceCollection.AddSingleton<LibraryWindow>();
        serviceCollection.AddSingleton<PosingOverlayWindow>();
        serviceCollection.AddSingleton<PosingOverlayToolbarWindow>();
        serviceCollection.AddSingleton<PosingTransformWindow>();
        serviceCollection.AddSingleton<PosingGraphicalWindow>();
        serviceCollection.AddSingleton<ImBrioText>();



        return serviceCollection;
    }

    public static bool TryGetService<T>(out T Tvalue) where T : notnull
    {
        if(_services is not null)
        {
            try
            {
                Tvalue = _services.GetRequiredService<T>();
                return true;
            }
            catch
            {
            }
        }

        Tvalue = default!;
        return false;
    }

    public void Dispose()
    {
        _services?.Dispose();
    }

    public void ToggleOverlay() {
        if(!TryGetService<IClientState>(out var clientState)) return;
        if(clientState.LocalPlayer == null) return;
        if(!TryGetService<EntityManager>(out var manager)) return;
        if(!manager.TryGetEntity(new EntityId(clientState.LocalPlayer), out var entity)) return;
        if(!entity.TryGetCapability<PosingCapability>(out var posingCapability)) return;
        manager.SetSelectedEntity(entity);
        posingCapability.OverlayOpen = !posingCapability.OverlayOpen;
    }

    public void ToggleDebugWindow() {
        if(!TryGetService<DebugWindow>(out var debugWindow)) return;
        debugWindow.Toggle();
    }
}
