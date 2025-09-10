using System.Numerics;

namespace LivePose.UI.Theming;

public static class ThemeManager
{

    public static Theme CurrentTheme { get; set; }

    static ThemeManager()
    {
        CurrentTheme = new Theme
        {
            Accent = new ThemeAccent
            {
                AccentColor = SetColor(new Vector4(98, 75, 224, 255)),
            }
        };
    }

    static uint SetColor(Vector4 colorVector)
    {
        uint r = (uint)(colorVector.X) & 0xFF;
        uint g = (uint)(colorVector.Y) & 0xFF;
        uint b = (uint)(colorVector.Z) & 0xFF;
        uint a = (uint)(colorVector.W) & 0xFF;

        return (a << 24) | (b << 16) | (g << 8) | r;
    }
}

public record class Theme
{
    public required ThemeAccent Accent;
}

public record class ThemeAccent
{
    public uint AccentColor = 0;
}
