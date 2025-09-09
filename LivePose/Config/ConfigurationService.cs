using Dalamud.Plugin;
using System;
using System.Collections.Generic;

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
        Configuration = _pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        if(Configuration.Posing.EnabledBoneCategories == null) {
            Configuration.Posing.EnabledBoneCategories = ["head", "body", "legs", "tail", "hands"];
        }
    }

    public void Save()
    {
        _pluginInterface.SavePluginConfig(Configuration);
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

    private static readonly string s_version = typeof(LivePosePlugin).Assembly.GetName().Version?.ToString() ?? "(Unknown Version)";

    public bool IsDebug => s_isDebug || Configuration.ForceDebug;
    public string Version => IsDebug ? "(Debug)" : $"v{s_version}";
}
