using Dalamud.Interface.Textures.TextureWraps;
using LivePose.UI.Windows;

namespace LivePose.Library;

public class LibraryRoot : GroupEntryBase
{
    public LibraryRoot()
        : base(null)
    {
    }

    public override string Name => "Library";
    public override IDalamudTextureWrap? Icon => null;

    public override void DrawInfo(LibraryWindow window)
    {
        base.DrawInfo(window);
    }

    protected override string GetpublicId()
    {
        return "Root";
    }
}
