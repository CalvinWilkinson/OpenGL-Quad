using System.Drawing;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace OpenGLQuad
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexData
    {
        public float X;

        public float Y;

        public float Z;

        public float Red;

        public float Green;

        public float Blue;

        public float Alpha;

        public float BatchIndex;

        public uint GetTotalBytes() => 8u * sizeof(float);

        public float[] ToArray() => new[]
        {
            X, Y, Z, Red, Green, Blue, Alpha, BatchIndex,
        };

        public void SetColor(Color color)
        {
            Red = color.R.MapValue(0f, 255f, 0f, 1f);
            Green = color.G.MapValue(0f, 255f, 0f, 1f);
            Blue = color.B.MapValue(0f, 255f, 0f, 1f);
            Alpha = color.A.MapValue(0f, 255f, 0f, 1f);
        }
    }
}
