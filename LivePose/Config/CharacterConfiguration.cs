using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LivePose.Config;
public sealed class NoCharacterConfiguration : CharacterConfiguration { }


public class CharacterConfiguration {
    public static readonly NoCharacterConfiguration None = new();
    public ulong ContentId { get; set; } = 0;
    public string Name { get; set; } = string.Empty;
    public uint World { get; set; } = 0;

    public DateTime SaveTime { get; set; } = DateTime.Now;

    public List<LivePoseCacheEntry> BodyPoses { get; set; } = [];
    
    public List<LivePoseCacheEntry> FacePoses { get; set; } = [];
    
    public List<LivePoseMinionEntry> MinionPoses { get; set; } = [];
    
    public void Save() {
        if(!LivePose.TryGetService(out ConfigurationService configurationService)) {
            LivePose.Log.Error("Failed to get ConfigurationService");
            return;
        }
        
        configurationService.SaveCharacterConfiguration(this.ContentId, this);
    }
}
