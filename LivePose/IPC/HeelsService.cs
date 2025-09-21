using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using LivePose.Entities;

namespace LivePose.IPC;

public class HeelsService : BrioIPC {
    public override string Name => "Simple Heels";

    public const string TagName = "LivePose_v2";
    
    

    public override bool IsAvailable => CheckStatus()  == IPCStatus.Available;

    public override bool AllowIntegration => true;
    
    public override (int Major, int Minor) GetAPIVersion() {
        try {
            return _apiVersion.InvokeFunc();
        } catch {
            return (-1, -1);
        }
    }
    public override IDalamudPluginInterface GetPluginInterface() {
        return _pluginInterface;
    }

    public override int APIMajor => 2;
    public override int APIMinor => 4;
    
    private ICallGateSubscriber<(int, int)> _apiVersion;
    private static ICallGateSubscriber<int, string, string, object?>? _setTag;
    private static ICallGateSubscriber<int, string, string?>? _getTag;
    private static ICallGateSubscriber<int, string, object?>? _removeTag;
    private static ICallGateSubscriber<int, string, string?, object?>? _tagChanged;

    private IDalamudPluginInterface _pluginInterface;
    private IPluginLog _pluginLog;
    private EntityManager _entityManager;
    private IClientState _clientState;
    private IFramework _framework;
    private IpcService _ipcService;
    
    public HeelsService(IDalamudPluginInterface pluginInterface, IPluginLog pluginLog, IClientState clientState, EntityManager entityManager, IFramework framework, IpcService ipcService) {

        _pluginInterface = pluginInterface;
        _pluginLog = pluginLog;
        _entityManager = entityManager;
        _clientState = clientState;
        _framework = framework;
        _ipcService = ipcService;
        
        _apiVersion = pluginInterface.GetIpcSubscriber<(int, int)>("SimpleHeels.ApiVersion");
        _setTag = pluginInterface.GetIpcSubscriber<int, string, string, object?>("SimpleHeels.SetTag");
        _getTag = pluginInterface.GetIpcSubscriber<int, string, string?>("SimpleHeels.GetTag");
        _removeTag = pluginInterface.GetIpcSubscriber<int, string, object?>("SimpleHeels.RemoveTag");
        _tagChanged = pluginInterface.GetIpcSubscriber<int, string, string?, object?>("SimpleHeels.TagChanged");
        
        _pluginLog.Debug("Subscribing to SimpleHeels.TagChanged");
        _tagChanged.Subscribe(OnTagChanged);
        
        _framework.RunOnTick(SetPlayerPoseTag, delayTicks: 10);
        
        _framework.RunOnTick(() => {
            for(ushort i = 2; i < 200; i++) {
                var tag = _getTag.InvokeFunc(i, TagName);
                if(tag != null) {
                    OnTagChanged(i, TagName, tag);
                }
            }
        }, delayTicks: 60);
    }


    private CancellationTokenSource? cancellationTokenSource;
    
    public void SetPlayerPoseTag() {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource = new CancellationTokenSource();

        var token = cancellationTokenSource.Token;

        _framework.RunOnTick(() => {
            if(token.IsCancellationRequested) return;
            
            LivePose.Log.Debug("Updating LivePose Tag for local player.");

            var data = _ipcService.GetPose(0);

            if(string.IsNullOrWhiteSpace(data)) {
                _removeTag?.InvokeAction(0, TagName);
                return;
            }
            
            _setTag?.InvokeAction(0, TagName, Compress(data));
            
        }, delay: TimeSpan.FromMilliseconds(500), cancellationToken: token);
    }

    private string Compress(string str) {
        byte[] compressedBytes;
        using (var uncompressedStream = new MemoryStream(Encoding.UTF8.GetBytes(str)))
        {
            using (var compressedStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Compress))
                {
                    uncompressedStream.CopyTo(gzipStream);
                }
                compressedBytes = compressedStream.ToArray();
            }
        }
        return Convert.ToBase64String(compressedBytes);
    }
    
    public static string Decompress(string? compressedString) {
        if(string.IsNullOrEmpty(compressedString)) return string.Empty;
        byte[] decompressedBytes;
        var compressedStream = new MemoryStream(Convert.FromBase64String(compressedString));
        using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
        {
            using (var decompressedStream = new MemoryStream())
            {
                gzipStream.CopyTo(decompressedStream);
                decompressedBytes = decompressedStream.ToArray();
            }
        }
        return Encoding.UTF8.GetString(decompressedBytes);
    }

    private void OnTagChanged(int objectIndex, string tag, string? value) {
        if(objectIndex < 2 || objectIndex >= 200) return;
        if(tag != TagName) return;
        LivePose.Log.Debug($"LivePose tag Changed for Object#{objectIndex}");
        var decompressedData = Decompress(value);
        LivePose.Log.Verbose($"Decompressed Tag: {value?.Length ?? 0} -> {decompressedData.Length}");
        _ipcService.SetPose((ushort)objectIndex, decompressedData);
    }

    public override void Dispose() {
        cancellationTokenSource?.Cancel();
        _tagChanged?.Unsubscribe(OnTagChanged);
    }
}
