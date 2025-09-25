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

    public Dictionary<ulong, CharacterConfiguration> Characters { get; private set; } = new();
    
    private readonly IDalamudPluginInterface _pluginInterface;

    public delegate void OnConfigurationChangedDelegate();
    public event OnConfigurationChangedDelegate? OnConfigurationChanged;

    public static ConfigurationService Instance { get; private set; } = null!;

    public string ConfigDirectory => _pluginInterface.ConfigDirectory.FullName;

    public string CharacterConfigDirectory => Path.Join(_pluginInterface.ConfigDirectory.FullName, "LivePoseCharacters");
    

    public ConfigurationService(IDalamudPluginInterface pluginInterface)
    {
        Instance = this;
        _pluginInterface = pluginInterface;

        Configuration = Load() ?? new Configuration();
        Configuration.Posing.EnabledBoneCategories ??= ["head", "body", "legs", "tail", "hands"];


        if(!Directory.Exists(CharacterConfigDirectory)) {
            Directory.CreateDirectory(CharacterConfigDirectory);
        }
        
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


    public CharacterConfiguration GetCharacterConfiguration(ulong contentId) {
        if(Characters.TryGetValue(contentId, out var characterConfiguration)) {
            return characterConfiguration;
        }
        
        var characterFile = Path.Join(CharacterConfigDirectory, $"{contentId:X16}.json");
        try {
            if(File.Exists(characterFile)) {
                var json = File.ReadAllText(characterFile);
                characterConfiguration = JsonConvert.DeserializeObject<CharacterConfiguration>(json);
                if(characterConfiguration != null) {
                    characterConfiguration.ContentId = contentId;
                    Characters[contentId] =  characterConfiguration;
                    return characterConfiguration;
                }
            }
        } catch(Exception ex) {
            LivePose.Log.Error(ex, $"Error loading character configuration {contentId:X}");
        }
        
        var newConfig = new CharacterConfiguration() {
            ContentId = contentId
        };
        
        Characters[contentId] = newConfig;
        return newConfig;
    }
    
    public void SaveCharacterConfiguration(ulong contentId, CharacterConfiguration characterConfiguration) {
        Characters[contentId] =  characterConfiguration;
        
        if (!Directory.Exists(CharacterConfigDirectory)) Directory.CreateDirectory(CharacterConfigDirectory);
        
        var characterFile = Path.Join(CharacterConfigDirectory, $"{contentId:X16}.json");

        characterConfiguration.SaveTime = DateTime.Now;
        
        var json = JsonConvert.SerializeObject(characterConfiguration, Formatting.Indented, new JsonSerializerSettings() {
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            TypeNameHandling = TypeNameHandling.None
        });

        File.WriteAllText(characterFile, json);
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
        
        if(!Directory.Exists(CharacterConfigDirectory)) Directory.CreateDirectory(CharacterConfigDirectory);

        foreach(var (cid, characterConfig) in Characters) {
            var characterFile = Path.Join(CharacterConfigDirectory, $"{cid:X16}.json");
            var characterJson = JsonConvert.SerializeObject(characterConfig, Formatting.Indented, new JsonSerializerSettings() {
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                TypeNameHandling = TypeNameHandling.None
            });
            
            File.WriteAllText(characterFile, characterJson);
        }
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
