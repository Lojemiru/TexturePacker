using System.IO.Compression;
using System.Text;

// TODO: Replace with Alex's even more up-to-date version and move away from consuming raw bytes for rendering 

namespace TexturePacker;

// I stole this from Alex's Gist:
// https://gist.github.com/AlexQ3D/f07ba048fd97bfbdca609a0742591d85
//
// I haven't modified anything yet, but I'm sure I'll have a cool rant here once I do.

// This .ase/.aseprite parser is taken from queercodedcoder
// https://gist.github.com/queercodedcoder/53f31e780e6096d7767047f96e263156
// 
// I've fixed a couple bugs - layer alpha blends now work correctly, and layers which
// are invisible in aseprite will also be invisible when imported. Any changes/fixes I've documented
// with a comment, so you can pitch them if I messed something up.
//
// Have fun! And if you fork it with fixes tag me... somehow!
// - Alex


// This .ase parser is taken from Noel Berry, one of the developers of Celeste.
// https://gist.github.com/NoelFB/778d190e5d17f1b86ebf39325346fcc5
// A little bit of research reveals that he used it as part of their asset
// pipeline: parsing .ase files into sliced regions & pivots, building
// a sprite atlas with the resulting data, and taking that atlas in-game.
// 
// We were looking to parse .ase files to achieve *literally* the same thing
// (using slices to generate sprite regions), so it's really lucky that he
// open sourced this thing. It mostly worked as-is, but indexed sprites
// weren't parsed properly, and there was a nasty bug that only showed up
// when you loaded a medium/large image. Those issues are patched here,
// and I'll do my best to update this script with any changes that we
// make in the future. Where possible, I've also tried to leave Noel's
// original comments intact. It's still just an ugly C# .ase parser :)
// 
// - mini

// Grayscale still untested
// Only implemented the stuff I needed / wanted, other stuff is ignored

// File Format:
// https://github.com/aseprite/aseprite/blob/master/docs/ase-file-specs.md


public struct Color
{
    public byte R;
    public byte G;
    public byte B;
    public byte A;

    public Color(int r, int g, int b, int a) 
    { 
        R = (byte)r;
        G = (byte)g;
        B = (byte)b;
        A = (byte)a;
    }

    // this premultiplies the alpha ...
    // depending on your use-case, you may not want this
    public static Color FromNonPremultiplied(int r, int g, int b, int a)
    {
        return new Color(
            (r * a / 255),
            (g * a / 255),
            (b * a / 255),
            a
        );
    }

}

public struct AsepritePoint
{
    public int X;
    public int Y;

    public AsepritePoint(int x, int y) { X = x; Y = y; }
}

    public class Aseprite
{

    public enum Modes
    {
        Indexed = 1,
        Grayscale = 2,
        RGBA = 4
    }

    private enum Chunks
    {
        OldPaletteA = 0x0004,
        OldPaletteB = 0x0011,
        Layer = 0x2004,
        Cel = 0x2005,
        CelExtra = 0x2006,
        ColorProfile = 0x2007,
        ExternalFiles = 0x2008,
        Mask = 0x2016,
        Path = 0x2017,
        FrameTags = 0x2018,
        Palette = 0x2019,
        UserData = 0x2020,
        Slice = 0x2022,
        Tileset = 0x2023,
    }

    public readonly Modes Mode;
    public readonly int Width;
    public readonly int Height;
    public readonly int FrameCount;
    public readonly int ColorPaletteCount;

    public List<Layer> Layers = new List<Layer>();
    public List<Frame> Frames = new List<Frame>();
    public List<Tag> Tags = new List<Tag>();
    public List<Slice> Slices = new List<Slice>();

    public Aseprite(Modes mode, int width, int height)
    {
        Mode = mode;
        Width = width;
        Height = height;
    }

    #region .ase Parser

    public Aseprite(string file, bool loadImageData = true)
        : this(File.OpenRead(file), loadImageData)
    {
    }
    public Aseprite(Stream stream, bool loadImageData = true)
    {
        try
        {
            var reader = new BinaryReader(stream);

            // wrote these to match the documentation names so it's easier (for me, anyway) to parse
            byte BYTE() { return reader.ReadByte(); }
            ushort WORD() { return reader.ReadUInt16(); }
            short SHORT() { return reader.ReadInt16(); }
            uint DWORD() { return reader.ReadUInt32(); }
            long LONG() { return reader.ReadInt32(); }
            string STRING() { return Encoding.UTF8.GetString(BYTES(WORD())); }
            byte[] BYTES(int number) { return reader.ReadBytes(number); }
            void SEEK(int number) { reader.BaseStream.Position += number; }

            int headerFlags;
            byte transparentPaletteIndex;

            // Header
            {
                // file size
                DWORD();

                // Magic number (0xA5E0)
                var magic = WORD();
                if (magic != 0xA5E0)
                    throw new Exception("File is not in .ase format");

                // Frames / Width / Height / Color Mode
                FrameCount = WORD();
                Width = WORD();
                Height = WORD();
                Mode = (Modes)(WORD() / 8);

                // Flags
                headerFlags = (int)DWORD();

                // Other Info, Ignored
                WORD();        // Speed (deprecated)
                DWORD();       // Set be 0
                DWORD();       // Set be 0

                // Palette entry
                transparentPaletteIndex = BYTE();

                SEEK(3);       // Ignore these bytes

                // Number of colors (0 means 256)
                ColorPaletteCount = WORD();
                
                // Other Info, Ignored
                BYTE();        // Pixel width
                BYTE();        // Pixel height
                SEEK(92);      // For Future
            }

            if (ColorPaletteCount == 0)
                ColorPaletteCount = 256;

            byte[] temp = new byte[Width * Height * (int)Mode];
            var palette = new Color[ColorPaletteCount];
            IUserData last = null;

            // Frames
            for (int i = 0; i < FrameCount; i++)
            {
                var frame = new Frame(this);
                if (loadImageData)
                    frame.Pixels = new Color[Width * Height];
                Frames.Add(frame);

                long frameStart, frameEnd;
                uint chunkCount;

                // frame header
                {
                    frameStart = reader.BaseStream.Position;
                    frameEnd = frameStart + DWORD();
                    WORD();                         // Magic number (always 0xF1FA)
                    chunkCount = WORD();            // Number of "chunks" in this frame
                    frame.Duration = WORD();        // Frame duration (in milliseconds)
                    SEEK(2);                        // For future (set to zero)
                    uint newChunkCount = DWORD();   // New chunk count value, if available
                    if (newChunkCount != 0)
                        chunkCount = newChunkCount;
                }

                // bool foundNewPalette = false;
                // bool foundOldPalette = false;

                // chunks
                for (uint j = 0; j < chunkCount; j++)
                {
                    long chunkStart, chunkEnd;
                    Chunks chunkType;

                    // chunk header
                    {
                        chunkStart = reader.BaseStream.Position;
                        chunkEnd = chunkStart + DWORD();
                        chunkType = (Chunks)WORD();
                    }

                    // LAYER CHUNK
                    if (chunkType == Chunks.Layer)
                    {
                        // create layer
                        var layer = new Layer();

                        // get layer data
                        layer.Flag = (Layer.Flags)WORD();
                        layer.Type = (Layer.Types)WORD();
                        layer.ChildLevel = WORD();
                        WORD(); // width (unused)
                        WORD(); // height (unused)
                        layer.BlendMode = WORD();
                        layer.Alpha = (BYTE() / 255f);
                        
                        // ALEX - Check if layer is visible, if yes, set it's alpha to zero
                        if ((layer.Flag & Layer.Flags.Visible) == 0) {
                            layer.Alpha = 0f;
                        }

                        SEEK(3); // for future
                        layer.Name = STRING();
                        

                        last = layer;
                        Layers.Add(layer);
                    }
                    // CEL CHUNK
                    else if (chunkType == Chunks.Cel)
                    {
                        // create cel
                        var cel = new Cel();

                        // get cel data
                        cel.Layer = Layers[WORD()];
                        cel.X = SHORT();
                        cel.Y = SHORT();
                        cel.Alpha = BYTE() / 255f;
                        var celType = WORD(); // type
                        SHORT(); // z index??
                        SEEK(5);

                        if (loadImageData)
                        {
                            // RAW or DEFLATE
                            if (celType == 0 || celType == 2)
                            {
                                cel.Width = WORD();
                                cel.Height = WORD();

                                var count = cel.Width * cel.Height * (int)Mode;
                                //temp = new byte[count];

                                // RAW
                                if (celType == 0)
                                {
                                    reader.Read(temp, 0, cel.Width * cel.Height * (int)Mode);
                                }
                                // DEFLATE
                                else
                                {
                                    // https://george.chiramattel.com/blog/2007/09/deflatestream-block-length-does-not-match.html
                                    // "...skipping past the first two bytes solved the problem. Those bytes are part of
                                    // the zlib specification (RFC 1950), not the deflate specification (RFC 1951).
                                    // Those bytes contain information about the compression method and flags."
                                    SEEK(2);
                                    
                                    var deflate = new DeflateStream(reader.BaseStream, CompressionMode.Decompress, true);
                                    int toRead = count;
                                    int readSoFar = 0;
                                    while (readSoFar != count)
                                        readSoFar += deflate.Read(temp, readSoFar, count - readSoFar);
                                }
                                
                                cel.Bytes = new byte[temp.Length];

                                for (var k = 0; k < temp.Length; k++)
                                {
                                    cel.Bytes[k] = temp[k];
                                }

                                cel.Pixels = new Color[cel.Width * cel.Height];
                                BytesToPixels(temp, cel.Pixels, Mode, palette);
                                CelToFrame(frame, cel);
                                
                            }
                            // REFERENCE
                            else if (celType == 1)
                            {
                                // not gonna worry about it
                                // LogMan.Info("DISASTER!!! Found a celType 1...");
                            }
                            else
                            {
                                // not gonna worry about it
                                // LogMan.Info("DISASTER!!! Found a celType other...");
                            }
                        }

                        last = cel;
                        frame.Cels.Add(cel);
                    }
                    // PALETTE CHUNK
                    else if (chunkType == Chunks.Palette)
                    {
                        var size = DWORD();
                        var start = DWORD();
                        var end = DWORD();
                        SEEK(8); // for future

                        for (int p = 0; p < (end - start) + 1; p++)
                        {
                            var hasName = WORD();
                            palette[start + p] = Color.FromNonPremultiplied(BYTE(), BYTE(), BYTE(), BYTE());
                            if (IsBitSet(hasName, 0))
                                STRING();
                        }

                        // foundNewPalette = true;
                    }
                    // USERDATA
                    else if (chunkType == Chunks.UserData)
                    {
                        if (last != null)
                        {
                            var flags = (int)DWORD();

                            // has text
                            if (IsBitSet(flags, 0))
                                last.UserDataText = STRING();

                            // has color
                            if (IsBitSet(flags, 1))
                                last.UserDataColor = Color.FromNonPremultiplied(BYTE(), BYTE(), BYTE(), BYTE());
                        }
                    }
                    // TAG
                    else if (chunkType == Chunks.FrameTags)
                    {
                        var count = WORD();
                        SEEK(8);

                        for (int t = 0; t < count; t++)
                        {
                            var tag = new Tag();
                            tag.From = WORD();
                            tag.To = WORD();
                            tag.LoopDirection = (Tag.LoopDirections)BYTE();
                            SEEK(8);
                            tag.Color = Color.FromNonPremultiplied(BYTE(), BYTE(), BYTE(), 255);
                            SEEK(1);
                            tag.Name = STRING();
                            Tags.Add(tag);
                        }
                    }
                    // SLICE
                    else if (chunkType == Chunks.Slice)
                    {
                        var count = DWORD();
                        var flags = (int)DWORD();
                        DWORD(); // reserved
                        var name = STRING();

                        for (int s = 0; s < count; s++)
                        {
                            var slice = new Slice();
                            slice.Name = name;
                            slice.Frame = (int)DWORD();
                            slice.OriginX = (int)LONG();
                            slice.OriginY = (int)LONG();
                            slice.Width = (int)DWORD();
                            slice.Height = (int)DWORD();

                            // 9 slice (ignored atm)
                            if (IsBitSet(flags, 0))
                            {
                                LONG();
                                LONG();
                                DWORD();
                                DWORD();
                            }

                            // pivot point
                            if (IsBitSet(flags, 1))
                                slice.Pivot = new AsepritePoint((int)LONG(), (int)LONG());

                            last = slice;
                            Slices.Add(slice);
                        }
                    }
                    else
                    {
                        // LogMan.Info("Read a chunk that we did not handle!");
                        // LogMan.Info($"{chunkType.ToString()}");

                        // if (chunkType == Chunks.OldPaletteA)
                        //     foundOldPalette = true;
                    }

                    reader.BaseStream.Position = chunkEnd;
                }

                // if (!foundNewPalette)
                // {
                //     LogMan.Info($"Palette: NO, Old Palette: {(foundOldPalette ? "YES" : "NO")}");
                // }

                reader.BaseStream.Position = frameEnd;
            }
        }
        finally { stream.Dispose(); }
    }

    #endregion

    #region Data Structures

    public class Frame
    {
        public Aseprite Sprite;
        public int Duration;
        public Color[] Pixels;
        public List<Cel> Cels;

        public Frame(Aseprite sprite)
        {
            Sprite = sprite;
            Cels = new List<Cel>();
        }
    }

    public class Tag
    {
        public enum LoopDirections
        {
            Forward = 0,
            Reverse = 1,
            PingPong = 2
        }

        public string Name;
        public LoopDirections LoopDirection;
        public int From;
        public int To;
        public Color Color;
    }

    public interface IUserData
    {
        string UserDataText { get; set; }
        Color UserDataColor { get; set; }
    }

    public struct Slice : IUserData
    {
        public int Frame;
        public string Name;
        public int OriginX;
        public int OriginY;
        public int Width;
        public int Height;
        public AsepritePoint? Pivot;
        public string UserDataText { get; set; }
        public Color UserDataColor { get; set; }
    }

    public class Cel : IUserData
    {
        public Layer Layer;
        public Color[] Pixels;
        public byte[] Bytes;

        public int X;
        public int Y;
        public int Width;
        public int Height;
        public float Alpha;

        public string UserDataText { get; set; }
        public Color UserDataColor { get; set; }
    }

    public class Layer : IUserData
    {
        [Flags]
        public enum Flags
        {
            Visible = 1,
            Editable = 2,
            LockMovement = 4,
            Background = 8,
            PreferLinkedCels = 16,
            Collapsed = 32,
            Reference = 64
        }

        public enum Types
        {
            Normal = 0,
            Group = 1
        }

        public Flags Flag;
        public Types Type;
        public string Name;
        public int ChildLevel;
        public int BlendMode;
        public float Alpha;

        public string UserDataText { get; set; }
        public Color UserDataColor { get; set; }
    }

    #endregion

    #region Blend Modes

    // Copied from Aseprite's source code:
    // https://github.com/aseprite/aseprite/blob/master/src/doc/blend_funcs.cpp

    private delegate void Blend(ref Color dest, Color src, byte opacity);


    
    private static Blend[] BlendModes = new Blend[]
    {
        // 0 - NORMAL
        (ref Color dest, Color src, byte opacity) => {
            // ALEX - a more direct port of the original aseprite blendmode. A little ugly, but fixes an issue
            // with un-premultiplied alpha that the original parser(s) have
            
            if ((dest.A) == 0) {
                int alpha = (int)(src.A * (opacity / 255f));
                dest = Color.FromNonPremultiplied(src.R,src.G,src.B,alpha);
                return;
            }
            
            if (src.A == 0) {
                return;
            }
            
            int Br = dest.R;
            int Bg = dest.G;
            int Bb = dest.B;
            int Ba = dest.A;
            int Sr = src.R;
            int Sg = src.G;
            int Sb = src.B;
            int Sa = src.A;
            Sa = MUL_UN8(Sa, opacity);
            
            int Ra = Sa + Ba - MUL_UN8(Ba, Sa);
            int Rr = Br + (Sr - Br) * Sa / Ra;
            int Rg = Bg + (Sg - Bg) * Sa / Ra;
            int Rb = Bb + (Sb - Bb) * Sa / Ra;
            dest = new Color(Rr, Rg, Rb, Ra);
        }
    };

    private static int MUL_UN8(int a, int b)
    {
        var t = (a * b) + 0x80;
        return (((t >> 8) + t) >> 8);
    }

    #endregion

    #region Utils

    /// <summary>
    /// Converts an array of Bytes to an array of Colors, using the specific Aseprite Mode & Palette
    /// </summary>
    private void BytesToPixels(byte[] bytes, Color[] pixels, Aseprite.Modes mode, Color[] palette)
    {
        int len = pixels.Length;
        if (mode == Modes.RGBA)
        {
            for (int p = 0, b = 0; p < len; p++, b += 4)
            {
                //Console.WriteLine(bytes[b] + "" + bytes[b + 1] + "" + bytes[b + 2] + "" + bytes[b + 3]);
                pixels[p].R = (byte)(bytes[b + 0] * bytes[b + 3] / 255);
                pixels[p].G = (byte)(bytes[b + 1] * bytes[b + 3] / 255);
                pixels[p].B = (byte)(bytes[b + 2] * bytes[b + 3] / 255);
                pixels[p].A = bytes[b + 3];
            }
        }
        else if (mode == Modes.Grayscale)
        {
            for (int p = 0, b = 0; p < len; p++, b += 2)
            {
                pixels[p].R = pixels[p].G = pixels[p].B = (byte)(bytes[b + 0] * bytes[b + 1] / 255);
                pixels[p].A = bytes[b + 1];
            }
        }
        else if (mode == Modes.Indexed)
        {
            for (int p = 0, b = 0; p < len; p++, b += 1)
            {
                byte index = bytes[b];
                
                // ALEX - add this check so we're not filling backgrounds with our "clear (index zero in a palette)" colour
                if (index != 0)
                    pixels[p] = palette[index];
            }
        }
    }

    /// <summary>
    /// Applies a Cel's pixels to the Frame, using its Layer's BlendMode & Alpha
    /// </summary>
    private void CelToFrame(Frame frame, Cel cel)
    {
        var opacity = (byte)((cel.Alpha * cel.Layer.Alpha) * 255);
        var blend = BlendModes[cel.Layer.BlendMode];

        for (int sx = 0; sx < cel.Width; sx++)
        {
            int dx = cel.X + sx;
            int dy = cel.Y * frame.Sprite.Width;

            for (int i = 0, sy = 0; i < cel.Height; i++, sy += cel.Width, dy += frame.Sprite.Width)
            {
                blend(ref frame.Pixels[dx + dy], cel.Pixels[sx + sy], opacity);
            }
        }
    }

    private static bool IsBitSet(int b, int pos)
    {
        return (b & (1 << pos)) != 0;
    }

    #endregion
}
