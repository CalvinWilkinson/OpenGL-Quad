using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Silk.NET.OpenGL;

namespace OpenGLQuad
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Maps the given <paramref name="value"/> from one range to another.
        /// </summary>
        /// <param name="value">The value to map.</param>
        /// <param name="fromStart">The from starting range value.</param>
        /// <param name="fromStop">The from ending range value.</param>
        /// <param name="toStart">The to starting range value.</param>
        /// <param name="toStop">The to ending range value.</param>
        /// <returns>A value that has been mapped to a range between <paramref name="toStart"/> and <paramref name="toStop"/>.</returns>
        public static float MapValue(this float value, float fromStart, float fromStop, float toStart, float toStop)
            => toStart + ((toStop - toStart) * ((value - fromStart) / (fromStop - fromStart)));

        /// <summary>
        /// Maps the given <paramref name="value"/> from one range to another.
        /// </summary>
        /// <param name="value">The value to map.</param>
        /// <param name="fromStart">The from starting range value.</param>
        /// <param name="fromStop">The from ending range value.</param>
        /// <param name="toStart">The to starting range value.</param>
        /// <param name="toStop">The to ending range value.</param>
        /// <returns>A value that has been mapped to a range between <paramref name="toStart"/> and <paramref name="toStop"/>.</returns>
        public static float MapValue(this byte value, float fromStart, float fromStop, float toStart, float toStop)
            => toStart + ((toStop - toStart) * ((value - fromStart) / (fromStop - fromStart)));

        public static float ToRadians(this float degrees) => degrees * (float)Math.PI / 180f;

        public static void LabelShader(this GL gl, uint shaderId, string label)
            => gl.ObjectLabel(ObjectIdentifier.Shader, shaderId, (uint)label.Length, label);

        public static void LabelShaderProgram(this GL gl, uint shaderId, string label)
            => gl.ObjectLabel(ObjectIdentifier.Program, shaderId, (uint)label.Length, label);

        public static void LabelVertexArray(this GL gl, uint vertexArrayId, string label)
            => gl.ObjectLabel(ObjectIdentifier.VertexArray, vertexArrayId, (uint)label.Length, label);

        public static void LabelBuffer(this GL gl, uint bufferId, string label)
            => gl.ObjectLabel(ObjectIdentifier.Buffer, bufferId, (uint)label.Length, label);

        public static void BeginGroup(this GL gl, string name)
        {
            if (gl is null)
            {
                throw new NullReferenceException("The GL object is null");
            }

            gl.PushDebugGroup(DebugSource.DebugSourceApplication, 100, (uint)name.Length, name);
        }

        public static void EndGroup(this GL gl)
        {
            if (gl is null)
            {
                throw new NullReferenceException("The GL object is null");
            }

            gl.PopDebugGroup();
        }

        public static uint TotalBytes(this IEnumerable<QuadData> data) => (uint)data.Sum(d => d.GetTotalBytes());

        public static float[] ToVertexArray(this IEnumerable<QuadData> quads)
        {
            var result = new List<float>();

            foreach (var quad in quads)
            {
                result.AddRange(quad.ToArray());
            }

            return result.ToArray();
        }

        public static Vector2 GetViewPortSize(this GL gl)
        {
            /*
             * [0] = X
             * [1] = Y
             * [3] = Width
             * [4] = Height
             */
            var data = new int[4];

            gl.GetInteger(GetPName.Viewport, data);

            return new Vector2(data[2], data[3]);
        }

        public static void SetViewPortSize(this GL gl, uint width, uint height)
        {
            /*
             * [0] = X
             * [1] = Y
             * [3] = Width
             * [4] = Height
             */
            var data = new int[4];

            gl.GetInteger(GetPName.Viewport, data);
            gl.Viewport(data[0], data[1], width, height);
        }
    }
}
