using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Dalamud.Configuration;
using Newtonsoft.Json;

namespace LivePose.Config;

public class ConfigurationService : IDisposable
{
    public Configuration Configuration { get; private set; } = null!;

    private readonly IDalamudPluginInterface _pluginInterface;

    public delegate void OnConfigurationChangedDelegate();
    public event OnConfigurationChangedDelegate? OnConfigurationChanged;

    public static ConfigurationService Instance { get; private set; } = null!;

    public string ConfigDirectory => _pluginInterface.ConfigDirectory.FullName;

    public ConfigurationService(IDalamudPluginInterface pluginInterface)
    {
        Instance = this;
        _pluginInterface = pluginInterface;

        Configuration = Load() ?? new Configuration();
        Configuration.Posing.EnabledBoneCategories ??= ["head", "body", "legs", "tail", "hands"];
    }

    private Configuration? Load() {
        var file = Path.Join(ConfigDirectory, "LivePose.Config.json");
        if(!File.Exists(file)) return null;
        var json = File.ReadAllText(file);
        return JsonConvert.DeserializeObject<Configuration>(json, new JsonSerializerSettings() {
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            TypeNameHandling = TypeNameHandling.None
        });
    }

    public void Save()
    {
        var file = Path.Join(ConfigDirectory, "LivePose.Config.json");
        if (!Directory.Exists(ConfigDirectory)) Directory.CreateDirectory(ConfigDirectory);
        
        var json = JsonConvert.SerializeObject(Configuration,  Formatting.Indented, new JsonSerializerSettings() {
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            TypeNameHandling = TypeNameHandling.None
        });
        
        File.WriteAllText(file, json);
    }

    public void ApplyChange(bool save = true)
    {
        if(save)
            Save();
        
        OnConfigurationChanged?.Invoke();
    }

    public void Reset()
    {
        Configuration = new Configuration();

        ApplyChange();
    }

    public void Dispose()
    {
        Save();
    }

#if DEBUG
    private static bool s_isDebug => true;
#else
    private static bool s_isDebug => false;
#endif

    private static readonly string s_version = typeof(LivePose).Assembly.GetName().Version?.ToString() ?? "(Unknown Version)";

    public bool IsDebug => s_isDebug || Configuration.ForceDebug;
    public string Version => IsDebug ? "(Debug)" : $"v{s_version}";
}
