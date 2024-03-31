using Newtonsoft.Json;

namespace TexturePacker.Models;

public class PageOptions
{
    [JsonProperty]
    public int Size = 2048;
    
    public static PageOptions FromJson(string json) => JsonConvert.DeserializeObject<PageOptions>(json, QuickType.Converter.Settings);
}