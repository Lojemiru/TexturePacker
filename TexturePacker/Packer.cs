using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using TexturePacker.Models;

namespace TexturePacker;

public static class Packer
{
    private static readonly List<Sprite> Sprites = new();
    private static readonly List<Frame> Frames = new();
    private static string _exportJson = "{\"sprites\":{\n";

    private const string ENUM_FILE_HEADER = @"
/*
 * WARNING: This file is auto-generated by the texture packer!!!
 *          Modifications to it will be overwritten the next time the texture packer is run.
 */


";

    public static void Enum(DirectoryInfo dir, FileSystemInfo output, string name, string nameSpace)
    {
        var enumOut = ENUM_FILE_HEADER + "namespace " + nameSpace + ";\n\npublic enum " + name + " {";

        List<string> entries = new();
        
        foreach (var file in dir.GetFiles())
        {
            var all = File.ReadAllText(file.FullName);
            var lines = all.Split("\n");
            foreach (var line in lines)
            {
                entries.Add(line);
            }
        }

        var finalEntries = entries.Distinct().ToList();

        foreach (var entry in finalEntries)
        {
            enumOut += "\n\t" + entry;
        }

        enumOut += "\n}";

        File.WriteAllText(output.FullName, enumOut);
    }

    public static void Pack(DirectoryInfo dir, int size, DirectoryInfo outDir, string name)
    {
        // Add sprites to list
        RecursiveAdd(dir);

        // Sort list from largest to smallest. I am lying to the computer here to get the largest images FIRST (inverting 1/-1).
        Frames.Sort(delegate(Frame x, Frame y)
        {
            // X is smaller
            if (x.Height < y.Height)
                return 1;

            // Y is smaller
            if (y.Height < x.Height)
                return -1;

            // Both are equal
            return 0;
        });

        Node root = new(new Rectangle(0, 0, size, size));

        var enumOutput = "";

        foreach (var frame in Frames)
        {
            var node = root.Insert(frame);
            if (node != null)
            {
                frame.Parent.Positions[frame.Index] = new FakeRectangle(node.Bounds.X + 1, node.Bounds.Y + 1, node.Bounds.Width - 2,
                    node.Bounds.Height - 2);
            }
        }

        foreach (var sprite in Sprites)
        {
            _exportJson += sprite + ",\n";
            enumOutput += sprite.Metadata.Name + ",\n";
        }

        _exportJson += "}}";

        File.WriteAllText(outDir + "/" + name + ".json", _exportJson);

        if (!Directory.Exists(outDir + "/enums"))
            Directory.CreateDirectory(outDir + "/enums");

        

        File.AppendAllText(outDir + "/enums/pages.enumPart", name + ",\n");

        if (!Directory.Exists(outDir + "/enums/sprites"))
            Directory.CreateDirectory(outDir + "/enums/sprites");
        
        File.WriteAllText(outDir + "/enums/sprites/" + name + ".enumPart", enumOutput);

        Image<Rgba32> canvas = new(size, size);

        root.Render(canvas);

        canvas.SaveAsPng(outDir + "/" + name + ".png");
    }

    public static void RecursiveAdd(DirectoryInfo directory)
    {
        FileInfo? metadata = null;
        List<string> names = new();
        List<Image> frames = new();

        foreach (var file in directory.GetFiles())
        {
            if (file.Extension == ".png")
                names.Add(file.FullName);
            else if (file.Name == "mdat.json")
                metadata = file;
        }
        
        names.Sort((strX, strY) =>
        {
            var x = int.Parse(Path.GetFileNameWithoutExtension(strX));
            var y = int.Parse(Path.GetFileNameWithoutExtension(strY));

            if (x == y) 
                return 0;

            return x < y ? -1 : 1;
        });

        var trueWidth = 0;
        var trueHeight = 0;
        
        var cropOffsets = new int[names.Count][];
        
        for (var n = 0; n < names.Count; n++)
        {
            var name = names[n];
            var img = Image.Load<Rgba32>(name);

            if (n == 0)
            {
                trueWidth = img.Width;
                trueHeight = img.Height;
            }
            
            var cropLeft = img.Width;
            var cropRight = 0;
            var cropTop = img.Height;
            var cropBottom = 0;

            for (var i = 0; i < img.Width; i++)
            {
                for (var j = 0; j < img.Height; j++)
                {
                    if (img[i, j].A <= 0) 
                        continue;
                    
                    if (i < cropLeft)
                        cropLeft = i;
                    if (i > cropRight)
                        cropRight = i;

                    if (j < cropTop)
                        cropTop = j;
                    if (j > cropBottom)
                        cropBottom = j;
                }
            }
            
            if (cropLeft != 0 || cropRight != img.Width || cropTop != 0 || cropBottom != img.Height)
                img.Mutate(x => x.Crop(new Rectangle(cropLeft, cropTop, 1 + cropRight - cropLeft, 1 + cropBottom - cropTop)).Pad(img.Width + 2, img.Height + 2));

            frames.Add(img);
            
            cropOffsets[n] = new[] { cropLeft, cropTop };
        }

        if (metadata != null)
        {
            var spr = new Sprite(metadata, trueWidth, trueHeight, cropOffsets);
            
            Sprites.Add(spr);
            for (var i = 0; i < frames.Count; i++)
            {
                Frames.Add(new Frame(frames[i], spr, i));
            }
        }

        foreach (var dir in directory.GetDirectories())
        {
            RecursiveAdd(dir);
        }
    }
}