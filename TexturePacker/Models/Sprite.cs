using Newtonsoft.Json;
using TexturePacker.QuickType;

namespace TexturePacker.Models;

internal sealed class Sprite
{
    public readonly Metadata Metadata;
    public readonly FakeRectangle[] Positions;
    private readonly int _width;
    private readonly int _height;
    private readonly int[][] CropOffsets;

    public Sprite(FileSystemInfo metadata, int width, int height, int[][] cropOffsets)
    {
        Metadata = Metadata.FromJson(File.ReadAllText(metadata.FullName));
        Positions = new FakeRectangle[Metadata.FrameCount];
        _width = width;
        _height = height;
        CropOffsets = cropOffsets;
    }

    public override string ToString()
    {
        var md = Export();

        return "\"" + Metadata.Name + "\":" + JsonConvert.SerializeObject(md);
    }

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
}