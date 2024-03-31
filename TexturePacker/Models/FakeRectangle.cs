

namespace TexturePacker.Models;

public class FakeRectangle
{
    public int X { get; set; }
    public int Y { get; set; }
    public int W { get; set; }
    public int H { get; set; }

    public FakeRectangle(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        W = width;
        H = height;
    }
}