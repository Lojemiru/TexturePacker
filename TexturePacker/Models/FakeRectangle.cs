

using SixLabors.ImageSharp;

namespace TexturePacker.Models;

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

    public Rectangle ToRectangle()
    {
        return new Rectangle(X, Y, Width, Height);
    }
}