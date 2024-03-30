using Newtonsoft.Json;
using TexturePacker.QuickType;

namespace TexturePacker.Models;

internal sealed class Sprite
{
    public readonly FakeRectangle[] Positions;
    private readonly int _width;
    private readonly int _height;
    private readonly int[][] CropOffsets;
    private string Name;

    public Sprite(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public override string ToString()
    {
        //var md = Export();

        return "\"" + Name + "\":";// + JsonConvert.SerializeObject(md);
    }

    /*
    private MetadataFinal Export()
    {
        MetadataFinal md = new()
        {
            Length = Metadata.FrameCount,
            OriginX = Metadata.Origin[0],
            OriginY = Metadata.Origin[1],
            AttachPoints = Metadata.AttachPoints,
            Positions = Positions,
            Width = _width,
            Height = _height,
            CropOffsets = CropOffsets
        };

        return md;
    }
    */
}