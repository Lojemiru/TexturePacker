using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TexturePacker.Models;

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
    
    #region Templated JSON nonsense

    private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        DateParseHandling = DateParseHandling.None,
        Converters =
        {
            new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
        },
    };

    public string ToJson() => JsonConvert.SerializeObject(this);

    #endregion
}