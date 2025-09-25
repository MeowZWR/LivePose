using System.Collections.Generic;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace LivePose.Resources;


public class TimelineIdentification {

    private IDataManager _dataManager;
    
    public TimelineIdentification(IDataManager dataManager) {
        _dataManager = dataManager;
        
        // Standing Idles
        basePoses.TryAdd(0, "No Pose");
        basePoses.TryAdd(3, "Standing Idle Pose #1");
        basePoses.TryAdd(3124, "Standing Idle Pose #2");
        basePoses.TryAdd(3126, "Standing Idle Pose #3");
        basePoses.TryAdd(3182, "Standing Idle Pose #4");
        basePoses.TryAdd(3184, "Standing Idle Pose #5");
        basePoses.TryAdd(7405, "Standing Idle Pose #6");
        basePoses.TryAdd(7407, "Standing Idle Pose #7");
        
        // Ground Sit
        basePoses.TryAdd(654, "Ground Sit Pose #1");
        basePoses.TryAdd(3136, "Ground Sit Pose #2");
        basePoses.TryAdd(3138, "Ground Sit Pose #3");
        basePoses.TryAdd(3171,  "Ground Sit Pose #4");
        
        // Chair Sit
        basePoses.TryAdd(643, "Chair Sit Pose #1");
        basePoses.TryAdd(3132, "Chair Sit Pose #2");
        basePoses.TryAdd(3134, "Chair Sit Pose #3");
        basePoses.TryAdd(8002, "Chair Sit Pose #4");
        basePoses.TryAdd(8004, "Chair Sit Pose #5");

        // Sleeping
        basePoses.TryAdd(585, "Sleeping Pose #1");
        basePoses.TryAdd(3140, "Sleeping Pose #2");
        basePoses.TryAdd(3142, "Sleeping Pose #3");
        
        // Parasol
        basePoses.TryAdd(7367, "Parasol Idle Pose #1");
        basePoses.TryAdd(8063, "Parasol Idle Pose #2");
        basePoses.TryAdd(8066, "Parasol Idle Pose #3");
        basePoses.TryAdd(8068, "Parasol Idle Pose #4");
        
        
        facialExpressions.TryAdd(0, "No Expression");
        foreach(var emote in dataManager.GetExcelSheet<Emote>()) {
            if(emote.EmoteCategory.RowId == 3) {
                facialExpressions.TryAdd(emote.ActionTimeline[0].RowId, emote.Name.ExtractText());
            }
        }
    }
    
    
    private readonly Dictionary<uint, string> facialExpressions = [];
    private readonly Dictionary<uint, string> basePoses = [];
    private readonly Dictionary<uint, string> upperBody = [];
    
    public string GetExpressionName(ushort timeline) {
        if(facialExpressions.TryGetValue(timeline, out var name)) return name;

        if(_dataManager.GetExcelSheet<ActionTimeline>().TryGetRow(timeline, out var row)) {
            return row.Key.ExtractText();
        }

        return GetKey(timeline);
    }

    private string GetKey(uint timeline) {
        if(_dataManager.GetExcelSheet<ActionTimeline>().TryGetRow(timeline, out var row)) {
            return row.Key.ExtractText();
        }
        
        return $"Timeline#{timeline}";
    }

    public string GetBodyPoseName((ushort timeline, ushort upperBodyTimeline) t) => GetBodyPoseName(t.timeline, t.upperBodyTimeline);
    
    public string GetBodyPoseName(ushort timeline, ushort upperBodyTimeline) {
        if(!basePoses.TryGetValue(timeline, out var name)) name = GetKey(timeline);
        if(upperBodyTimeline == 0) return name;
        if(upperBody.TryGetValue(upperBodyTimeline, out var upperBodyName)) return $"{upperBodyName} while {name}";
        return $"{GetKey(upperBodyTimeline)} while {name}";
    }
    
}
