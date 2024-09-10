using Newtonsoft.Json;

namespace TexturePacker;

public class PageOptions
{
    [JsonProperty]
    public int Size = 2048;

    [JsonProperty]
    public bool PackIndexed = false;

    [JsonProperty]
    public string[] IndexingExcludedNames = Array.Empty<string>();

    [JsonProperty]
    public int IndexingEqualityThreshold = 16;

    public static PageOptions FromJson(string json) => JsonConvert.DeserializeObject<PageOptions>(json);

    public override string ToString()
    {
        var mode = PackIndexed ? "Indexed Mode" : "Normal Mode"; 
        return $"[{mode}; page size {Size}]";
    }
}