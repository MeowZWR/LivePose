using Dalamud.Configuration;

namespace LivePose.Config;

public class Configuration : IPluginConfiguration
{
    public const int CurrentVersion = 3;
    public const int CurrentPopupKey = 16;

    public int Version { get; set; } = CurrentVersion;

    // First Time User
    public int PopupKey { get; set; } = -1;

    // Posing
    public PosingConfiguration Posing { get; set; } = new PosingConfiguration();

    public SceneImportConfiguration Import { get; set; } = new SceneImportConfiguration();

    // Library
    public LibraryConfiguration Library { get; set; } = new LibraryConfiguration();
    
    public string LastExportPath { get; set; } = string.Empty;

    public string LastScenePath { get; set; } = string.Empty;

    public bool SceneDestoryActorsBeforeImport { get; set; } = false;

    // Developer
    public bool ForceDebug { get; set; } = false;
}

public enum OpenBrioBehavior
{
    Manual,
    OnGPoseEnter,
    OnPluginStartup
}

public enum ApplyNPCHack
{
    Disabled,
    InGPose,
    Always
}
