using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace TexturePacker.Models;

internal sealed class Node
{
    private Node? _left;
    private Node? _right;
    public Rectangle Bounds;
    private Image? _sprite;

    public Node(Rectangle bounds)
    {
        Bounds = bounds;
    }

    public Node? Insert(Frame frame)
        => Insert(frame.Data);
    
    public Node? Insert(Image sprite)
    {
        // If we already have an image, or the image doesn't fit...
        if (_sprite != null || !Fits(sprite))
            // ...attempt to insert right, then attempt to insert left, and finally return null if neither of those worked.
            return _right?.Insert(sprite) ?? _left?.Insert(sprite);
            
        // Otherwise, the image fits so we should take it and try to create more space for other images.
        _sprite = sprite;

        // Width still has room - create new Node
        if (Bounds.Width - _sprite.Width > 0)
            _right = new Node(new Rectangle(Bounds.X + _sprite.Width, Bounds.Y, Bounds.Width - _sprite.Width, _sprite.Height));

        // Height still has room - create new Node
        if (Bounds.Height - _sprite.Height > 0)
            _left = new Node(new Rectangle(Bounds.X, Bounds.Y + _sprite.Height, Bounds.Width, Bounds.Height - _sprite.Height));

        // Set bounds to match sprite
        Bounds = new Rectangle(Bounds.X, Bounds.Y, _sprite.Width, _sprite.Height);

        return this;
    }

    public void Render(Image canvas)
    {
        if (_sprite != null)
        {
            canvas.Mutate(c => c.DrawImage(_sprite, new Point(Bounds.X, Bounds.Y), 1f));
        }

        _left?.Render(canvas);
        _right?.Render(canvas);
    }

    private bool Fits(Image sprite)
    {
        var bounds = sprite.Bounds;
        bounds.X = Bounds.X;
        bounds.Y = Bounds.Y;
        return Bounds.Contains(bounds);
    }
}