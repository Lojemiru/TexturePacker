using Newtonsoft.Json;

namespace TexturePacker.Models;

public sealed class Sprite
{
    // Layer, frame
    [JsonProperty("P")]
    public readonly FakeRectangle[][] Positions;
    
    [JsonProperty("W")]
    public readonly int Width;
    
    [JsonProperty("H")]
    public readonly int Height;
    
    // Layer, frame, then x/y as indexes 0 and 1
    [JsonProperty("C")]
    public int[][][]? CropOffsets;
    
    internal readonly string Name;
    
    [JsonProperty("L")]
    public readonly int Length;
    
    internal readonly int Layers;
    
    [JsonProperty("X")]
    public int OriginX;
    
    [JsonProperty("Y")]
    public int OriginY;
    
    [JsonProperty("A")]
    public readonly Dictionary<string, int[][]> AttachPoints = new ();

    public Sprite(string name, int width, int height, int layers, int length)
    {
        Name = name;
        Width = width;
        Height = height;
        CropOffsets = new int[layers][][];
        Positions = new FakeRectangle[layers][];
        Layers = layers;
        Length = length;
    }

    public override string ToString()
    {
        if (NullCropOffsets())
            CropOffsets = null;

        return "\"" + Name + "\":"+ JsonConvert.SerializeObject(this);
    }

    private bool NullCropOffsets()
    {
        foreach (var layer in CropOffsets!)
        {
            foreach (var frame in layer)
            {
                if (frame[0] != 0 || frame[1] != 0)
                {
                    return false;
                }
            }
        }

        return true;
    }
}