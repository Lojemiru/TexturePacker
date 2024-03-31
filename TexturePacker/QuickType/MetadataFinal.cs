using Newtonsoft.Json;
using TexturePacker.Models;

namespace TexturePacker.QuickType;

public class MetadataFinal
{
    [JsonProperty("L")]
    public int Length { get; set; }

    [JsonProperty("X")]
    public int OriginX { get; set; }

    [JsonProperty("Y")]
    public int OriginY { get; set; }

    [JsonProperty("A")]
    public Dictionary<string, int[][]> AttachPoints { get; set; }

    [JsonProperty("P")]
    public FakeRectangle[][] Positions { get; set; }

    [JsonProperty("W")]
    public int Width { get; set; }
        
    [JsonProperty("H")]
    public int Height { get; set; }
        
    [JsonProperty("C")]
    public int[][][]? CropOffsets { get; set; }

    public static MetadataFinal FromJson(string json) => JsonConvert.DeserializeObject<MetadataFinal>(json, QuickType.Converter.Settings);
}