namespace TexturePacker.QuickType;

using System.Collections.Generic;

using Newtonsoft.Json;

public sealed class Metadata
{
    [JsonProperty("frameCount")]
    public int FrameCount { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("origin")]
    public int[] Origin { get; set; }

    [JsonProperty("attachPoints")]
    public Dictionary<string, int[][]> AttachPoints { get; set; }

    public static Metadata FromJson(string json) => JsonConvert.DeserializeObject<Metadata>(json, QuickType.Converter.Settings);
}