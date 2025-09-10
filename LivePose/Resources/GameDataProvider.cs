using Dalamud.Plugin.Services;
using LivePose.Game.Actor.Appearance;
using LivePose.Resources.Sheets;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using System.Linq;
using Glasses = Lumina.Excel.Sheets.Glasses;

namespace LivePose.Resources;

public class GameDataProvider
{
    public static GameDataProvider Instance { get; private set; } = null!;

    public IDataManager DataManager { get; private set; }
    
    public readonly IReadOnlyDictionary<uint, Weather> Weathers;
    public readonly IReadOnlyDictionary<uint, Companion> Companions;
    public readonly IReadOnlyDictionary<uint, Ornament> Ornaments;
    public readonly IReadOnlyDictionary<uint, Mount> Mounts;
    public readonly IReadOnlyDictionary<uint, Festival> Festivals;
    public readonly IReadOnlyDictionary<uint, Status> Statuses;
    public readonly IReadOnlyDictionary<uint, BrioActionTimeline> ActionTimelines;
    public readonly IReadOnlyDictionary<uint, ENpcBase> ENpcBases;
    public readonly IReadOnlyDictionary<uint, ENpcResident> ENpcResidents;
    public readonly IReadOnlyDictionary<uint, BNpcBase> BNpcBases;
    public readonly IReadOnlyDictionary<uint, BNpcName> BNpcNames;
    public readonly IReadOnlyDictionary<uint, Stain> Stains;
    public readonly IReadOnlyDictionary<uint, Glasses> Glasses;
    

    public readonly HumanData HumanData;

    public GameDataProvider(IDataManager dataManager, ResourceProvider _resourceProvider)
    {
        Instance = this;
        
        Weathers = dataManager.GetExcelSheet<Weather>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();
        
        Companions = dataManager.GetExcelSheet<Companion>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

        Ornaments = dataManager.GetExcelSheet<Ornament>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

        Mounts = dataManager.GetExcelSheet<Mount>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

        Festivals = dataManager.GetExcelSheet<Festival>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

        Statuses = dataManager.GetExcelSheet<Status>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

        ActionTimelines = dataManager.GetExcelSheet<BrioActionTimeline>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();
        
        ENpcBases = dataManager.GetExcelSheet<ENpcBase>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

        ENpcResidents = dataManager.GetExcelSheet<ENpcResident>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

        BNpcBases = dataManager.GetExcelSheet<BNpcBase>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();
        
        BNpcNames = dataManager.GetExcelSheet<BNpcName>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

        Stains = dataManager.GetExcelSheet<Stain>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

        Glasses = dataManager.GetExcelSheet<Glasses>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

        HumanData = new HumanData(dataManager.GetFile("chara/xls/charamake/human.cmp")!.Data);

        DataManager = dataManager;
    }
}
