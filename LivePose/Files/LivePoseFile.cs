using System;
using System.IO;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;
using LivePose.Capabilities.Posing;
using LivePose.Config;
using LivePose.Entities;
using LivePose.Entities.Actor;
using LivePose.Game.Posing;
using LivePose.Library.Sources;
using LivePose.Resources;
using LivePose.UI.Controls.Editors;
using LivePose.UI.Controls.Stateless;
using Newtonsoft.Json;

namespace LivePose.Files;

public class LivePoseFileInfo : AppliableActorFileInfoBase<LivePoseFile>
{
    private PosingService _posingService;

    public override string Name => "Live Pose File";
    public override IDalamudTextureWrap Icon => ResourceProvider.Instance.GetResourceImage("Images.FileIcon_Unknown.png");
    public override string Extension => ".livepose";

    public LivePoseFileInfo(EntityManager entityManager, PosingService posingService, ConfigurationService configurationService)
        : base(entityManager, configurationService)
    {
        _posingService = posingService;
    }

    public override void DrawActions(FileEntry fileEntry, bool isModal)
    {
        if(ImBrio.Button("##pose_import_options_action", FontAwesomeIcon.Cog, new Vector2(25, 0), hoverText: "Import Options"))
        {
            ImGui.OpenPopup("import_options_popup_lib");
        }

        using(var popup = ImRaii.Popup("import_options_popup_lib"))
        {
            if(popup.Success)
            {
                PosingEditorCommon.DrawImportOptionEditor(_posingService.DefaultImporterOptions);
            }
        }

        ImGui.SameLine();

        base.DrawActions(fileEntry, isModal);
    }

    protected override void Apply(LivePoseFile file, ActorEntity actor, bool asExpression)
    {
        PosingCapability? capability;
        if(actor.TryGetCapability<PosingCapability>(out capability) && capability != null)
        {
            capability.ImportPose(file, asExpression: asExpression);
        }
    }

    public override object? Load(string filePath) {
        if(!File.Exists(filePath)) return null;
        var json = File.ReadAllText(filePath);
        return JsonConvert.DeserializeObject<LivePoseFile>(json);
    }
}


[Serializable]
public class LivePoseFile : JsonDocumentBase {
    public string TypeName { get; set; } = "Live Pose";

    public LivePoseData Data { get; set; } = new LivePoseData();

}
