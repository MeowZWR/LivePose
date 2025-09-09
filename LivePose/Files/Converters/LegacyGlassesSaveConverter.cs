using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LivePose.Files.Converters;

public class LegacyGlassesSaveConverter : JsonConverter<AnamnesisCharaFile.GlassesSave>
{
    public override AnamnesisCharaFile.GlassesSave Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        try
        {
            ushort? str = reader.GetUInt16();

            return new AnamnesisCharaFile.GlassesSave { GlassesId = (byte)str.Value };
        }
        catch(Exception)
        {
            LivePosePlugin.Log.Fatal($"Loaded GS Exception -- {typeToConvert}");

            return new AnamnesisCharaFile.GlassesSave { GlassesId = 0 };
        }
    }

    public override void Write(Utf8JsonWriter writer, AnamnesisCharaFile.GlassesSave value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
