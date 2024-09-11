using System.IO.Compression;
using System.Text;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using TexturePacker.Models;

namespace TexturePacker;

public static class AsepriteGenerator
{
    public static void Generate(Sprite sprite, string output)
    {
        var stream = File.OpenWrite(output);
        var writer = new BinaryWriter(stream);
        
        // HEADER
        WriteHeader(sprite, writer);

        // FRAMES
        
        // Frame 1 - chunks count is layer definitions (normal and origin/attach points), cels, origin, and attach points
        //var frame1 = WriteFrameHeader(sprite, writer, (sprite.Layers * 2) + 2 + (sprite.AttachPoints.Count * 2));
        var frame1 = WriteFrameHeader(sprite, writer, sprite.GetChunkCount(0) * 2);
        
        // First frame is special: it holds a set of layer chunks that define the layers layout.
        WriteLayerChunks(sprite, writer);
        
        // Then, we obviously need all cels in frame 0.
        WriteCelChunks(sprite, writer, 0);
        
        RecordLength(writer, frame1);
        
        // Loop over all remaining frames and write them out!
        if (sprite.Length > 1)
        {
            for (var i = 1; i < sprite.Length; i++)
            {
                // For each subsequent frame, we just count cels, origin, and attach points.
                var frameI = WriteFrameHeader(sprite, writer, sprite.GetChunkCount(i));
                
                WriteCelChunks(sprite, writer, i);

                RecordLength(writer, frameI);
            }
        }

        // Record actual length, skip to start of stream and overwrite placeholder.
        RecordLength(writer, 0);
        
        writer.Flush();
        writer.Dispose();
    }
    
    private static void WriteHeader(Sprite sprite, BinaryWriter writer)
    {
        // DWORD       File size
        // Overwritten after all other generation.
        writer.Write((uint)128);
        // WORD        Magic number (0xA5E0)
        //writer.Write(0xA5E0);
        writer.Write((ushort)0xA5E0);
        // WORD        Frames
        writer.Write((ushort)sprite.Positions[0].Length);
        // WORD        Width in pixels
        writer.Write((ushort)sprite.Width);
        // WORD        Height in pixels
        writer.Write((ushort)sprite.Height);
        // WORD        Color depth (bits per pixel)
        //               32 bpp = RGBA
        //               16 bpp = Grayscale
        //               8 bpp = Indexed
        // We ONLY support RGBA.
        writer.Write((ushort)32);
        // DWORD       Flags:
        //             1 = Layer opacity has valid value
        writer.Write((uint)1);
        // WORD        Speed (milliseconds between frame, like in FLC files)
        //             DEPRECATED: You should use the frame duration field
        //             from each frame header
        writer.Write((ushort)0);
        // DWORD       Set be 0
        writer.Write((uint)0);
        // DWORD       Set be 0
        writer.Write((uint)0);
        // BYTE        Palette entry (index) which represent transparent color
        //             in all non-background layers (only for Indexed sprites).
        // We do not support indexed mode. Stub out.
        writer.Write((byte)0);
        // BYTE[3]     Ignore these bytes
        // Hopefully these are ACTUALLY unused...
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((byte)0);
        // WORD        Number of colors (0 means 256 for old sprites)
        // Hoping this doesn't break anything horribly.
        writer.Write((ushort)255);
        // BYTE        Pixel width (pixel ratio is "pixel width/pixel height").
        //             If this or pixel height field is zero, pixel ratio is 1:1
        writer.Write((byte)0);
        // BYTE        Pixel height
        writer.Write((byte)0);
        // SHORT       X position of the grid
        writer.Write((short)0);
        // SHORT       Y position of the grid
        writer.Write((short)0);
        // WORD        Grid width (zero if there is no grid, grid size
        //             is 16x16 on Aseprite by default)
        writer.Write((ushort)16);
        // WORD        Grid height (zero if there is no grid)
        writer.Write((ushort)16);
        // BYTE[84]    For future (set to zero)
        for (var i = 0; i < 84; i++)
            writer.Write((byte)0);
    }

    private static int WriteFrameHeader(Sprite sprite, BinaryWriter writer, int chunks)
    {
        var index = writer.BaseStream.Position;

        // DWORD       Bytes in this frame
        // We will overwrite this later with the recorded index.
        writer.Write((uint)128);
        // WORD        Magic number (always 0xF1FA)
        writer.Write((ushort)0xF1FA);
        // WORD        Old field which specifies the number of "chunks"
        //             in this frame. If this value is 0xFFFF, we might
        //             have more chunks to read in this frame
        //             (so we have to use the new field)
        writer.Write((ushort)chunks);
        // WORD        Frame duration (in milliseconds)
        writer.Write((ushort)100);
        // BYTE[2]     For future (set to zero)
        writer.Write((byte)0);
        writer.Write((byte)0);
        // DWORD       New field which specifies the number of "chunks"
        //             in this frame (if this is 0, use the old field)
        // As far as I understand it, this will ALWAYS be equal to our layer count - for our purposes, at least.
        writer.Write((uint)chunks);

        return (int)index;
    }

    private static void RecordLength(BinaryWriter writer, int index)
    {
        var length = writer.BaseStream.Length - index;
        writer.Seek(index, SeekOrigin.Begin);
        writer.Write((uint)length);
        writer.Seek(0, SeekOrigin.End);
    }

    private static void WriteLayerChunks(Sprite sprite, BinaryWriter writer)
    {
        for (var i = 0; i < sprite.Layers; i++)
        {
            WriteLayerChunk(sprite, writer, $"Layer {i}");
        }
        
        foreach (var ap in sprite.AttachPoints.Keys)
        {
            WriteLayerChunk(sprite, writer, $"_attach_{ap}");
        }
        
        WriteLayerChunk(sprite, writer, "_origin");
    }

    private static void WriteLayerChunk(Sprite sprite, BinaryWriter writer, string name)
    {
        WriteChunk(sprite, writer, 0x2004, () =>
        {
            // WORD          Flags:
            //               1 = Visible
            //               2 = Editable
            //               4 = Lock movement
            //               8 = Background
            //               16 = Prefer linked cels
            //               32 = The layer group should be displayed collapsed
            //               64 = The layer is a reference layer
            // Layer should be both visible and editable, we do not care about anything else.
            writer.Write((ushort)3);
            // WORD        Layer type
            //               0 = Normal (image) layer
            //               1 = Group
            //               2 = Tilemap
            // Force normal layer, we don't use anything else.
            writer.Write((ushort)0);
            // WORD        Layer child level (see NOTE.1)
            // We are not a child of anything.
            writer.Write((ushort)0);
            // WORD        Default layer width in pixels (ignored)
            writer.Write((ushort)0);
            // WORD        Default layer height in pixels (ignored)
            writer.Write((ushort)0);
            // WORD        Blend mode (always 0 for layer set)
            //                Normal         = 0
            //                Multiply       = 1
            //                Screen         = 2
            //                Overlay        = 3
            //                Darken         = 4
            //                Lighten        = 5
            //                Color Dodge    = 6
            //                Color Burn     = 7
            //                Hard Light     = 8
            //                Soft Light     = 9
            //                Difference     = 10
            //                Exclusion      = 11
            //                Hue            = 12
            //                Saturation     = 13
            //                Color          = 14
            //                Luminosity     = 15
            //                Addition       = 16
            //                Subtract       = 17
            //                Divide         = 18
            // Force normal.
            writer.Write((ushort)0);
            // BYTE        Opacity
            //                Note: valid only if file header flags field has bit 1 set
            writer.Write((byte)255);
            // BYTE[3]     For future (set to zero)
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((byte)0);
            // STRING      Layer name
            writer.Write((ushort)name.Length);
            writer.Write(Encoding.ASCII.GetBytes(name));
            // + If layer type = 2
            // DWORD     Tileset index
            // I think I can ignore this with no consequence.
        });
    }

    private static void WriteCelChunks(Sprite sprite, BinaryWriter writer, int frame)
    {
        var count = 0;
        
        for (var i = 0; i < sprite.Layers; i++)
        {
            WriteCelChunk(sprite, writer, i, sprite.FrameToBytes(frame, i));
            count++;
        }
        
        foreach (var ap in sprite.AttachPoints.Values)
        {
            if (frame < ap.Length)
                WriteCelChunk(sprite, writer, count, sprite.DataPointBytes(), ap[frame][0], ap[frame][1], 1, 1);
            
            count++;
        }
        
        if (frame == 0) 
            WriteCelChunk(sprite, writer, count, sprite.DataPointBytes(), sprite.OriginX, sprite.OriginY, 1, 1);
    
    }

    private static void WriteCelChunk(Sprite sprite, BinaryWriter writer, int layer, byte[] imgData, int x = 0, int y = 0, int width = -1, int height = -1)
    {
        WriteChunk(sprite, writer, 0x2005, () =>
        {
            // WORD        Layer index (see NOTE.2)
            writer.Write((ushort)layer);
            // SHORT       X position
            writer.Write((short)x);
            // SHORT       Y position
            writer.Write((short)y);
            // BYTE        Opacity level
            writer.Write((byte)255);
            // WORD        Cel Type
            //             0 - Raw Image Data (unused, compressed image is preferred)
            //             1 - Linked Cel
            //             2 - Compressed Image
            //             3 - Compressed Tilemap
            writer.Write((ushort)2);
            // SHORT       Z-Index (see NOTE.5)
            //             0 = default layer ordering
            //             +N = show this cel N layers later
            //             -N = show this cel N layers back
            writer.Write((short)0);
            // BYTE[5]     For future (set to zero)
            for (var j = 0; j < 5; j++)
                writer.Write((byte)0);
            //  + For cel type = 2 (Compressed Image)
            //  WORD       Width in pixels
            writer.Write((ushort)(width == -1 ? sprite.Width : width));
            //  WORD       Height in pixels
            writer.Write((ushort)(height == -1 ? sprite.Height : height));
            //  PIXEL[]    "Raw Cel" data compressed with ZLIB method (see NOTE.3)
            var stream = new MemoryStream();
            var outputStream = new MemoryStream();
            using var inputStream = new DeflaterOutputStream(outputStream);
            inputStream.Write(imgData);
            inputStream.Flush();
            writer.Write(outputStream.ToArray());
        });
    }

    private static void WriteChunk(Sprite sprite, BinaryWriter writer, int type, Action writeContent)
    {
        var index = writer.BaseStream.Length;
        
        // DWORD       Chunk size
        // As usual, we will skip back and overwrite this.
        writer.Write((uint)128);
        // WORD        Chunk type
        writer.Write((ushort)type);
        // BYTE[]      Chunk data
        writeContent();

        // Jump to start of frame and write the frame length.
        RecordLength(writer, (int)index);
    }
}