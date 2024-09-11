using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace TexturePacker.Models;

public sealed class TexturePage
{
    /// <summary>
    /// Collection of all <see cref="Sprite"/>s contained within this <see cref="TexturePage"/>.
    /// </summary>
    [JsonProperty("sprites")]
    public Dictionary<string, Sprite> Sprites { get; private set; }
    
    [JsonProperty("options")]
    public PageOptions Options { get; private set; }

    public Image<Rgba32> Texture;

    public Image<Rgba32>[] Palettes; 

    public string Name;
    
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

    private static TexturePage FromJson(string json) 
        => JsonConvert.DeserializeObject<TexturePage>(json, Settings);

    #endregion

    public static TexturePage Load(string path, string name)
    {
        string fileText;
        TexturePage output;

        // Try to load file as text...
        try
        {
            fileText = File.ReadAllText($"{path}/{name}.json");
        }
        catch (FileNotFoundException e)
        {
            throw new FileNotFoundException("Unable to find metadata file for page \"" + name + "\"\n" + e.StackTrace);
        }

        // Then try to convert to JSON...
        try
        {
            output = FromJson(fileText);
        }
        catch (JsonReaderException e)
        {
            throw new JsonReaderException("Error loading JSON for page \"" + name + "\":" + e.Message + "\n" + e.StackTrace);
        }

        // Ensure we didn't get handed null.
        if (output is null)
        {
            throw new NullReferenceException("Error loading metadata file for page \"" + name + "\": JSON conversion returned null.");
        }

        // Then try to convert said FileStream to the actual Texture2D...
        try
        {
            output.Texture = Image.Load<Rgba32>($"{path}/{name}.png");
        }
        catch (InvalidOperationException e)
        {
            throw new InvalidOperationException($"Error loading texture for page \"{name}\":\n" + e.StackTrace);
        }
        
        foreach (var spr in output.Sprites)
        {
            spr.Value.Name = spr.Key;
            spr.Value.Atlas = output;
        }

        output.Name = name;

        return output;
    }

    public override string ToString()
    {
        return $"{Name} ({Sprites.Count} sprites)";
    }

    public void LoadPalettes(string path)
    {
        Console.WriteLine($"\tPage {Name} is indexed! Loading palettes...");
        var files = Directory.GetFiles(path).Select(Path.GetFileName).ToArray();
        Array.Sort(files);

        var names = files.Where(file => file.StartsWith(Name)).ToList();

        Palettes = new Image<Rgba32>[names.Count];

        for (var i = 0; i < names.Count; i++)
        {
            Palettes[i] = Image.Load<Rgba32>($"{path}/{names[i]}");
            Console.WriteLine($"\tFound palette {names[i]}. Storing as index {i}.");
        }
    }

    public void WriteToFolder(string path)
    {
        var folder = $"{path}/{Name}";
        if (Directory.Exists(folder))
            Directory.Delete(folder, true);
        
        Directory.CreateDirectory(folder);
        
        File.WriteAllText($"{folder}/PageOptions.json", Options.ToJson());

        foreach (var sprite in Sprites.Values)
        {
            sprite.WriteToDirectory(folder);
        }
    }
}