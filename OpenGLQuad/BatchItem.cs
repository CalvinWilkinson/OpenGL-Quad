using System.Drawing;
using System.Numerics;

namespace OpenGLQuad
{
    public struct BatchItem
    {
        public Vector2 Position;

        public uint Width;

        public uint Height;

        public Color Color;

        public static BatchItem Create() => default;

        public bool IsEmpty()
        {
            return Position == Vector2.Zero &&
                   Width == 0 &&
                   Height == 0 &&
                   Color.IsEmpty;
        }

        public void Empty()
        {
            Position = Vector2.Zero;
            Width = 0;
            Height = 0;
            Color = Color.Empty;
        }
    }
}
