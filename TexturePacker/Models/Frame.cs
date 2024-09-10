using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace TexturePacker.Models;

internal sealed class Frame
{
    public readonly Image<Rgba32> Data;
    public readonly Sprite Parent;
    public readonly int Index;
    public readonly int Layer;

    public int Width => Data.Width;
    public int Height => Data.Height;

    public Frame(Image<Rgba32> data, Sprite parent, int index, int layer)
    {
        Data = data;
        Parent = parent;
        Index = index;
        Layer = layer;
    }
}