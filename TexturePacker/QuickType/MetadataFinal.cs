using Newtonsoft.Json;
using TexturePacker.Models;

namespace TexturePacker.QuickType;

public class MetadataFinal
{
    [JsonProperty("length")]
    public int Length { get; set; }

    [JsonProperty("originX")]
    public int OriginX { get; set; }

    [JsonProperty("originY")]
    public int OriginY { get; set; }

    [JsonProperty("attachPoints")]
    public Dictionary<string, int[][]> AttachPoints { get; set; }

    [JsonProperty("positions")]
    public FakeRectangle[] Positions { get; set; }

    [JsonProperty("width")]
    public int Width { get; set; }
        
    [JsonProperty("height")]
    public int Height { get; set; }
        
    [JsonProperty("cropOffsets")]
    public int[][] CropOffsets { get; set; }

    public static MetadataFinal FromJson(string json) => JsonConvert.DeserializeObject<MetadataFinal>(json, QuickType.Converter.Settings);
}