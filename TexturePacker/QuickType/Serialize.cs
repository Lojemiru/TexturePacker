using Newtonsoft.Json;

namespace TexturePacker.QuickType;

public static class Serialize
{
    public static string ToJson(this Metadata self) => JsonConvert.SerializeObject(self, QuickType.Converter.Settings);
    public static string ToJson(this MetadataFinal self) => JsonConvert.SerializeObject(self, QuickType.Converter.Settings);
}