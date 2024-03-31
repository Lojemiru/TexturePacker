using Newtonsoft.Json;
using TexturePacker.QuickType;

namespace TexturePacker.Models;

internal sealed class Sprite
{
    // Layer, frame
    public readonly FakeRectangle[][] Positions;
    private readonly int _width;
    private readonly int _height;
    // Layer, frame, then x/y as indexes 0 and 1
    public readonly int[][][] CropOffsets;
    public readonly string Name;
    public readonly int Length;
    public readonly int Layers;
    public int OriginX;
    public int OriginY;
    public readonly Dictionary<string, int[][]> AttachPoints = new ();

    public Sprite(string name, int width, int height, int layers, int length)
    {
        Name = name;
        _width = width;
        _height = height;
        CropOffsets = new int[layers][][];
        Positions = new FakeRectangle[layers][];
        Layers = layers;
        Length = length;
    }

    public override string ToString()
    {
        var md = Export();

        return "\"" + Name + "\":"+ JsonConvert.SerializeObject(md);
    }

    private bool NullCropOffsets()
    {
        foreach (var layer in CropOffsets)
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
    
    private MetadataFinal Export()
    {
        MetadataFinal md = new()
        {
            Length = Length,
            OriginX = OriginX,
            OriginY = OriginY,
            AttachPoints = AttachPoints,
            Positions = Positions,
            Width = _width,
            Height = _height,
            CropOffsets = NullCropOffsets() ? null : CropOffsets
        };

        return md;
    }
}