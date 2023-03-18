﻿using Codeuctivity.ImageSharpCompare;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Parsing;
using System.Drawing;
using Point = SixLabors.ImageSharp.Point;
using Rectangle = SixLabors.ImageSharp.Rectangle;
using QuickType;
using System.Text.Json.Nodes;
using Newtonsoft.Json;
using System.Diagnostics;

// Full disclosure - no code was directly used, but I poked through these as my learning material:
// https://blackpawn.com/texts/lightmaps/default.html
// https://gist.github.com/ttalexander2/88a40eec0fd0ea5b31cc2453d6bbddad


// TODO: Duplicate removal
// TODO: Excess transparency trimming
class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Texture packer. Shenanigans abound!!!");

        var enumCommand = new Command("enum", "Compiles a folder of enum files into a single target enum .cs file with the given namespace.");

        var dirOption = new Option<DirectoryInfo?>(
            name: "--input",
            description: "The directory to grab enum .cs files from.");

        var outputOption = new Option<FileInfo?>(
            name: "--output",
            description: "The output file.");

        var namespaceOption = new Option<string>(
            name: "--namespace",
            description: "The namespace for the output enums.");

        var nameOption = new Option<string>(
            name: "--name",
            description: "The name of the output file."
            );

        enumCommand.AddOption(dirOption);
        enumCommand.AddOption(outputOption);
        enumCommand.AddOption(namespaceOption);
        enumCommand.AddOption(nameOption);

        enumCommand.SetHandler((dir, output, name, nameSpace) =>
        {
            Enum(dir!, output!, name!, nameSpace!);
        },
            dirOption, outputOption, nameOption, namespaceOption);

        rootCommand.AddCommand(enumCommand);

        var packCommand = new Command("pack", "Packs textures from a folder into a texturepage, JSON metadata, and an enum file.");

        var dirOption2 = new Option<DirectoryInfo?>(
            name: "--input",
            description: "The directory to begin packing from.");

        var sizeOption = new Option<int>(
            name: "--size",
            description: "The size of the output texturepage, in pixels.",
            getDefaultValue: () => 2048);

        var outputOption2 = new Option<DirectoryInfo?>(
            name: "--output",
            description: "The output directory for the resulting texturepage.");

        var nameOption2 = new Option<string>(
            name: "--name",
            description: "The name of the output file."
            );

        packCommand.AddOption(dirOption2);
        packCommand.AddOption(sizeOption);
        packCommand.AddOption(outputOption2);
        packCommand.AddOption(nameOption2);

        packCommand.SetHandler((dir, size, output, name) =>
        {
            Pack(dir!, size!, output!, name!);
        },
            dirOption2, sizeOption, outputOption2, nameOption2);


        rootCommand.AddCommand(packCommand);

        return await rootCommand.InvokeAsync(args);
    }

    

    static List<Image> Images = new();
    static List<Sprite> Sprites = new();
    static string ExportJson = "{\"sprites\":{\n";

    static readonly string EnumFileHeader = @"
/*
 * WARNING: This file is auto-generated by the texture packer!!!
 *          Modifications to it will be overwritten the next time the texture packer is run.
 */


";

    static void Enum(DirectoryInfo dir, FileInfo output, string name, string nameSpace)
    {
        string enumOut = EnumFileHeader + "namespace " + nameSpace + " {\npublic enum " + name + " {\n";

        List<string> entries = new();
        
        foreach (FileInfo file in dir.GetFiles())
        {
            var all = File.ReadAllText(file.FullName);
            var lines = all.Split("\n");
            foreach (string line in lines)
            {
                entries.Add(line);
            }
        }

        List<string> finalEntries = entries.Distinct().ToList();

        foreach (string entry in finalEntries)
        {
            enumOut += entry + "\n";
        }

        enumOut += "}\n}";

        File.WriteAllText(output.FullName, enumOut);
    }

    static void Pack(DirectoryInfo dir, int size, DirectoryInfo outDir, string name)
    {
        // Add sprites to list
        RecursiveAdd(dir);

        // Sort list from largest to smallest. I am lying to the computer here to get the largest images FIRST (inverting 1/-1).
        Sprites.Sort(delegate(Sprite x, Sprite y)
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

        string enumOutput = "";

        foreach (var sprite in Sprites)
        {
            for (var i = 0; i < sprite.Metadata.FrameCount; i++)
            {
                Image frame = sprite.Frames[i]; 
                Node? node = root.Insert(frame);
                if (node != null)
                {
                    sprite.Positions[i] = new(node.Bounds.X, node.Bounds.Y, node.Bounds.Width, node.Bounds.Height);
                }
            }
            ExportJson += sprite.ToString() + ",\n";
            enumOutput += "\n\t" + sprite.Metadata.Name + ",";
        }

        ExportJson += "}}";

        File.WriteAllText(outDir + "/" + name + ".json", ExportJson);

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

    static void RecursiveAdd(DirectoryInfo directory)
    {
        FileInfo? metadata = null;
        List<string> names = new();
        List<Image> frames = new();

        foreach (FileInfo file in directory.GetFiles())
        {
            if (file.Extension == ".png")
            {
                names.Add(file.FullName);
                //frames.Add(Image.Load<Rgba32>(file.FullName));
            }
            else if (file.Name == "mdat.json")
                metadata = file;
        }
        
        names.Sort(delegate (string x, string y)
        {
            var _x = int.Parse(Path.GetFileNameWithoutExtension(x));
            var _y = int.Parse(Path.GetFileNameWithoutExtension(y));

            if (_x == _y) return 0;

            return _x < _y ? -1 : 1;
        });

        foreach (string name in names)
        {
            if (names.Count > 9)
                Console.WriteLine("ADDING FRAME: " + name);
            frames.Add(Image.Load<Rgba32>(name));
        }

        if (metadata != null)
            Sprites.Add(new Sprite(metadata, frames));

        foreach (DirectoryInfo dir in directory.GetDirectories())
        {
            RecursiveAdd(dir);
        }
    }

    class Sprite
    {
        public Metadata Metadata;
        public List<Image> Frames;
        public FakeRectangle[] Positions;
        public int Width;
        public int Height;

        public Sprite(FileInfo metadata, List<Image> frames)
        {
            Metadata = Metadata.FromJson(File.ReadAllText(metadata.FullName));
            Frames = frames;
            Positions = new FakeRectangle[Metadata.FrameCount];
            Width = frames[0].Width;
            Height = frames[0].Height;
        }

        public override string ToString()
        {
            MetadataFinal md = Export();

            return "\"" + Metadata.Name + "\":" + JsonConvert.SerializeObject(md);
        }

        public MetadataFinal Export()
        {
            MetadataFinal md = new()
            {
                Length = Metadata.FrameCount,
                OriginX = Metadata.Origin[0],
                OriginY = Metadata.Origin[1],
                AttachPoints = Metadata.AttachPoints,
                Positions = Positions,
                Width = Width,
                Height = Height
            };

            return md;
        }
    }

    class Node
    {
        public Node? Left;
        public Node? Right;
        public Rectangle Bounds;
        public Image? Sprite;

        public Node(Rectangle bounds)
        {
            Bounds = bounds;
        }

        public Node? Insert(Image sprite)
        {
            // First case - image fits!
            if (Sprite == null && Fits(sprite))
            {
                Sprite = sprite;

                // Width still has room - create new Node
                if (Bounds.Width - Sprite.Width > 0)
                    Right = new Node(new Rectangle(Bounds.X + Sprite.Width, Bounds.Y, Bounds.Width - Sprite.Width, Sprite.Height));

                // Height still has room - create new Node
                if (Bounds.Height - Sprite.Height > 0)
                    Left = new Node(new Rectangle(Bounds.X, Bounds.Y + Sprite.Height, Bounds.Width, Bounds.Height - Sprite.Height));

                // Set bounds to match sprite
                Bounds = new Rectangle(Bounds.X, Bounds.Y, Sprite.Width, Sprite.Height);

                return this;
            }

            // Second case - image does not fit, Right exists, so insert
            if (Right != null)
            {
                Node? right = Right.Insert(sprite);
                if (right != null)
                    return right;
            }

            // Third case - image does not fit, Right was null or something, Left exists, so insert
            if (Left != null)
                return Left.Insert(sprite);

            return null;
        }

        public void Render(Image canvas)
        {
            if (Sprite != null)
            {
                canvas.Mutate(c => c.DrawImage(Sprite, new Point(Bounds.X, Bounds.Y), 1f));
            }

            if (Left != null)
                Left.Render(canvas);

            if (Right != null)
                Right.Render(canvas);
        }

        private bool Fits(Image sprite)
        {
            Rectangle bounds = sprite.Bounds();
            bounds.X = Bounds.X;
            bounds.Y = Bounds.Y;
            return Bounds.Contains(bounds);
        }
    }

}

// <auto-generated />
//
// To parse this JSON data, add NuGet 'Newtonsoft.Json' then do:
//
//    using QuickType;
//
//    var metadata = Metadata.FromJson(jsonString);


namespace QuickType
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using System.Numerics;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public partial class Metadata
    {
        [JsonProperty("frameCount")]
        public int FrameCount { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("origin")]
        public int[] Origin { get; set; }

        [JsonProperty("attachPoints")]
        public Dictionary<string, int[][]> AttachPoints { get; set; }

        public static Metadata FromJson(string json) => JsonConvert.DeserializeObject<Metadata>(json, QuickType.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this Metadata self) => JsonConvert.SerializeObject(self, QuickType.Converter.Settings);
        public static string ToJson(this MetadataFinal self) => JsonConvert.SerializeObject(self, QuickType.Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }


    public partial class MetadataFinal
    {
        [JsonProperty("length")]
        public int Length { get; set; }

        [JsonProperty("originX")]
        public int OriginX { get; set; }

        [JsonProperty("originY")]
        public int OriginY { get; set; }

        [JsonProperty("attachPoints")]
        public Dictionary<string, int[][]> AttachPoints { get; set; }

        [JsonProperty("positions")]
        public FakeRectangle[] Positions { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }
        
        [JsonProperty("height")]
        public int Height { get; set; }

        public static MetadataFinal FromJson(string json) => JsonConvert.DeserializeObject<MetadataFinal>(json, QuickType.Converter.Settings);
    }

    public class FakeRectangle
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public FakeRectangle(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
}


/*
namespace QuickType
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public partial class Metadata
    {
        [JsonProperty("frameCount")]
        public int FrameCount { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("origin")]
        public int[] Origin { get; set; }

        [JsonProperty("attachPoints")]
        public Dictionary<string, int[][]> AttachPoints { get; set; }
    
        public static Metadata FromJson(string json) => JsonConvert.DeserializeObject<Metadata>(json, QuickType.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this Metadata self) => JsonConvert.SerializeObject(self, QuickType.Converter.Settings);
        public static string ToJson(this MetadataFinal self) => JsonConvert.SerializeObject(self, QuickType.Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }


    public partial class MetadataFinal
    {
        [JsonProperty("frameCount")]
        public int FrameCount { get; set; }

        [JsonProperty("origin")]
        public int[] Origin { get; set; }

        [JsonProperty("attachPoints")]
        public Dictionary<string, int[][]> AttachPoints { get; set; }

        [JsonProperty("positions")]
        public int[][] Positions { get; set; }
        
        [JsonProperty("size")]
        public int[] Size { get; set; }

        public static MetadataFinal FromJson(string json) => JsonConvert.DeserializeObject<MetadataFinal>(json, QuickType.Converter.Settings);
    }
}
*/