using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using TexturePacker.Models;

namespace TexturePacker;

public static class ImageExtensions
{
    /// <summary>
    /// Crops this image and sets the relevant crop offset data to its parent <see cref="Sprite"/>.
    /// </summary>
    /// <param name="img">The image to crop.</param>
    /// <param name="cel">The Cel from which this image originates.</param>
    /// <param name="parent">The parent Sprite of this image.</param>
    /// <param name="layerIndex">The layer index of this image.</param>
    /// <param name="index">The frame index of this image.</param>
    public static void Crop(this Image<Rgba32> img, Aseprite.Cel cel, Sprite parent, int layerIndex, int index)
    {
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
            img.Mutate(x => x.Crop(new Rectangle(cropLeft, cropTop, 
                    1 + cropRight - cropLeft, 1 + cropBottom - cropTop))
                .Pad(img.Width, img.Height));

        parent.CropOffsets![layerIndex][index] = new[] { cropLeft + cel.X, cropTop + cel.Y };
    }
}