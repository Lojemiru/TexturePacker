using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

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
    
    internal string Name;
    
    [JsonProperty("L")]
    public readonly int Length;
    
    internal readonly int Layers;
    
    [JsonProperty("X")]
    public int OriginX;
    
    [JsonProperty("Y")]
    public int OriginY;
    
    [JsonProperty("A")]
    public readonly Dictionary<string, int[][]> AttachPoints = new ();

    internal TexturePage Atlas;

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
    
    [JsonConstructor]
    public Sprite([JsonProperty("L")] int length, [JsonProperty("X")] int originX,
        [JsonProperty("Y")] int originY, [JsonProperty("A")] Dictionary<string, int[][]> attachPoints,
        [JsonProperty("P")] FakeRectangle[][] positions, [JsonProperty("W")] int width,
        [JsonProperty("H")] int height, [JsonProperty("C")] int[][][]? cropOffsets)
    {
        Length = length;
        OriginX = originX;
        OriginY = originY;
        AttachPoints = attachPoints;
        Positions = positions;
        Width = width;
        Height = height;
        CropOffsets = cropOffsets;
        Layers = positions.Length;
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

    public void WriteToDirectory(string dir)
    {
        AsepriteGenerator.Generate(this, $"{dir}/{Name}.aseprite");
    }

    public byte[] FrameToBytes(int frame, int layer)
    {
        var fakeRectangle = Positions[layer][frame];
        var img = new Image<Rgba32>(Width, Height);

        img.Mutate(c => c.DrawImage(Atlas.Texture, fakeRectangle.ToRectangle(), PixelColorBlendingMode.Normal, PixelAlphaCompositionMode.Src, 1f));

        // Adjust image position based on crop offsets.
        if (CropOffsets is not null)
        {
            var x = CropOffsets[layer][frame][0];
            var y = CropOffsets[layer][frame][1];
            var copy = new Image<Rgba32>(Width, Height);
            
            copy.Mutate(c => c.DrawImage(img, new Point(x, y), PixelColorBlendingMode.Normal, PixelAlphaCompositionMode.Src, 1f));
            img = copy;
        }

        // Restore original colors.
        if (Atlas.Options.PackIndexed && !Atlas.Options.IndexingExcludedNames.Contains(Name))
        {
            for (var i = 0; i < img.Height; i++)
            {
                for (var j = 0; j < img.Width; j++)
                {
                    if (img[j, i].A <= 0)
                        continue;

                    img[j, i] = Atlas.Palettes[layer][0, img[j, i].R];
                }
            }
        }

        return ImageToBytes(img);
    }

    public byte[] DataPointBytes()
    {
        var img = new Image<Rgba32>(1, 1);

        img[0, 0] = new Rgba32(0, 0, 0, 255);

        return ImageToBytes(img);
    }

    public int GetChunkCount(int frame)
    {
        var output = Layers;
        
        // Origin
        if (frame == 0)
            output++;

        foreach (var ap in AttachPoints.Values)
        {
            if (frame < ap.Length)
                output++;
        }

        return output;
    }

    private static byte[] ImageToBytes(Image<Rgba32> image)
    {
        var output = new byte[image.Width * image.Height * 4];
        var current = 0;
        
        for (var i = 0; i < image.Height; i++)
        {
            for (var j = 0; j < image.Width; j++)
            {
                var pixel = image[j, i];

                output[current] = pixel.R;
                output[current + 1] = pixel.G;
                output[current + 2] = pixel.B;
                output[current + 3] = pixel.A;
                
                current += 4;
            }
        }

        return output;
    }
}