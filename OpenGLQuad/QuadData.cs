using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpenGLQuad
{
    [StructLayout(LayoutKind.Sequential)]
    public struct QuadData
    {
        public VertexData Vertex1;

        public VertexData Vertex2;

        public VertexData Vertex3;

        public VertexData Vertex4;

        public uint GetTotalBytes()
        {
            return Vertex1.GetTotalBytes() +
                   Vertex2.GetTotalBytes() +
                   Vertex3.GetTotalBytes() +
                   Vertex4.GetTotalBytes();
        }

        public float[] ToArray()
        {
            var result = new List<float>();

            result.AddRange(Vertex1.ToArray());
            result.AddRange(Vertex2.ToArray());
            result.AddRange(Vertex3.ToArray());
            result.AddRange(Vertex4.ToArray());

            return result.ToArray();
        }
    }
}
