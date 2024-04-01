using SixLabors.ImageSharp;

namespace TexturePacker.Models;

internal sealed class Frame
{
    public readonly Image Data;
    public readonly Sprite Parent;
    public readonly int Index;
    public readonly int Layer;

    public int Width => Data.Width;
    public int Height => Data.Height;

    public Frame(Image data, Sprite parent, int index, int layer)
    {
        Data = data;
        Parent = parent;
        Index = index;
        Layer = layer;
    }
}