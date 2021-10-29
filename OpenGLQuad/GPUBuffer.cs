using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using Silk.NET.OpenGL;
// ReSharper disable InconsistentNaming

namespace OpenGLQuad
{
    public class GPUBuffer : IDisposable
    {
        private readonly GL _gl;
        private uint _vao; // Vertex Array Object
        private uint _vbo; // Vertex Buffer Object
        private uint _ebo; // Element Buffer Object
        private QuadData[] _quadData;
        private uint[] _indices;

        private readonly uint _batchSize;
        private Dictionary<uint,uint> _transformLocations = new();
        private readonly uint _shaderId;

        public GPUBuffer(GL gl, uint shaderId, uint batchSize)
        {
            _gl = gl;
            _shaderId = shaderId;
            _batchSize = batchSize;

            // Generate the VAO and VBO with only 1 object each
            _gl.GenVertexArrays(1, out _vao);
            _gl.LabelVertexArray(_vao, "VAO");

            _gl.GenBuffers(1, out _vbo);
            _gl.LabelBuffer(_vbo, "VBO");

            _gl.GenBuffers(1, out _ebo);
            _gl.LabelBuffer(_ebo, "EBO");

            SetupData();
        }

        public uint TotalIndices => (uint)_indices.Length;

        private void SetupData()
        {
            _gl.BeginGroup("Setup Data");
            _gl.BeginGroup("Upload Vertex Data");

            BindVAO();
            BindVBO();

            _quadData = GenerateQuadData();

            var vertBufferData = _quadData.ToVertexArray();
            var vertData = new ReadOnlySpan<float>(vertBufferData);
            _gl.BufferData(BufferTargetARB.ArrayBuffer, _quadData.TotalBytes(), vertData, BufferUsageARB.DynamicDraw);

            _gl.EndGroup();

            _gl.BeginGroup("Upload Indices Data");
            BindEBO();

            _indices = GenerateIndices();

            var indicesData = new ReadOnlySpan<uint>(_indices);

            // Configure the Vertex Attribute so that OpenGL knows how to read the VBO
            _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(sizeof(uint) * _indices.Length), indicesData, BufferUsageARB.StaticDraw);
            _gl.EndGroup();

            SetupVAO();

            _gl.BeginGroup("Get Transform Locations");

            for (var i = 0u; i < _batchSize; i++)
            {
                _transformLocations.Add(i, (uint)_gl.GetUniformLocation(_shaderId, $"u_transform[{i.ToString()}]"));
            }

            UnbindVBO();
            UnbindVAO();
            UnbindEBO();
            _gl.EndGroup();
            _gl.EndGroup();
        }

        private void SetupVAO()
        {
            _gl.BeginGroup("Setup Vertex Attributes");
            unsafe
            {
                var stride = 8u * sizeof(float);
                var vertexSize = 3u * sizeof(float);
                // Vertex
                _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);
                _gl.EnableVertexAttribArray(0);

                // Color
                var colorOffset = vertexSize;
                var colorSize = 4u * sizeof(float);
                _gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, stride, (void*)colorOffset);
                _gl.EnableVertexAttribArray(1);

                // Batch Index
                var batchIndexOffset = vertexSize + colorSize;
                _gl.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, stride, (void*)batchIndexOffset);
                _gl.EnableVertexAttribArray(2);
            }
            _gl.EndGroup();
        }

        public void UpdateTransformData(Vector2 position, uint width, uint height, uint batchIndex)
        {
            var viewPortSize = _gl.GetViewPortSize();
            var transform = BuildTransformationMatrix(
                viewPortSize,
                position.X,
                position.Y,
                width,
                height,
                1f,
                0f);

            unsafe
            {
                _gl.UniformMatrix4((int)_transformLocations[batchIndex], 1u, true, (float*)&transform);
            }
        }

        public void UpdateVertexData(Color color, uint batchIndex)
        {
            var quadData = _quadData[batchIndex];
            quadData.Vertex1.SetColor(color);
            quadData.Vertex2.SetColor(color);
            quadData.Vertex3.SetColor(color);
            quadData.Vertex4.SetColor(color);

            var totalBytes = quadData.GetTotalBytes();
            var data = new ReadOnlySpan<QuadData>(new[] {quadData});
            var offset = totalBytes * batchIndex;

            BindVBO();

            unsafe
            {
                fixed (void* dataPtr = data)
                {
                    _gl.BufferSubData(BufferTargetARB.ArrayBuffer, (nint)offset, totalBytes, dataPtr);
                }
            }

            UnbindVBO();
        }

        private QuadData[] GenerateQuadData()
        {
            var result = new List<QuadData>();

            for (var i = 0u; i < _batchSize; i++)
            {
                result.AddRange(new QuadData[]
                {
                    new ()
                    {
                        Vertex1 = new VertexData
                        {
                            X = -1.0f, Y = 1.0f, Z = 0,
                            Red = 1.0f, Green = 1.0f, Blue = 1.0f, Alpha = 1.0f,
                            BatchIndex = i,
                        },
                        Vertex2 = new VertexData
                        {
                            X = -1.0f, Y = -1.0f, Z = 0f,
                            Red = 1.0f, Green = 1f, Blue = 1f, Alpha = 0f,
                            BatchIndex = i,
                        },
                        Vertex3 = new VertexData
                        {
                            X = 1.0f, Y = 1.00f, Z = 0f,
                            Red = 1.0f, Green = 1f, Blue = 1f, Alpha = 0f,
                            BatchIndex = i,
                        },
                        Vertex4 = new VertexData
                        {
                            X = 1.0f, Y = -1.0f, Z = 0f,
                            Red = 1.0f, Green = 1f, Blue = 1f, Alpha = 0f,
                            BatchIndex = i,
                        }
                    }
                });
            }

            return result.ToArray();
        }

        private uint[] GenerateIndices()
        {
            var result = new List<uint>();

            for (var i = 0u; i < _batchSize; i++)
            {
                var maxIndex = result.Count <= 0 ? 0 : result.Max() + 1;

                result.AddRange(new uint[]
                {
                    maxIndex,
                    maxIndex + 1,
                    maxIndex + 2,
                    maxIndex + 2,
                    maxIndex + 1,
                    maxIndex + 3,
                });
            }

            return result.ToArray();
        }

        private void BindVBO() => _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

        private void UnbindVBO() => _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0); // Unbind the VBO

        private void BindEBO() => _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);

        /// <summary>
        /// NOTE: Make sure to unbind AFTER you unbind the VAO.  This is because the EBO is stored
        /// inside of the VAO.  Unbinding the EBO before unbinding, (or without unbinding the VAO),
        /// you are telling OpenGL that you don't want your VAO to use the EBO.
        /// </summary>
        private void UnbindEBO() => _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);

        public void BindVAO() => _gl.BindVertexArray(_vao);

        private void UnbindVAO() => _gl.BindVertexArray(0); // Unbind the VAO

        public void Dispose()
        {
            _gl.DeleteVertexArray(_vao);
            _gl.DeleteBuffer(_vbo);
            _gl.DeleteBuffer(_ebo);
        }

        private Matrix4x4 BuildTransformationMatrix(Vector2 viewPortSize, float x, float y, uint width, uint height, float size, float angle)
        {
            if (viewPortSize.X <= 0)
            {
                throw new ArgumentException("The port size width cannot be a negative or zero value.");
            }

            if (viewPortSize.Y <= 0)
            {
                throw new ArgumentException("The port size height cannot be a negative or zero value.");
            }

            var scaleX = width / viewPortSize.X;
            var scaleY = height / viewPortSize.Y;

            scaleX *= size;
            scaleY *= size;

            var ndcX = x.MapValue(0f, viewPortSize.X, -1f, 1f);
            var ndcY = y.MapValue(0f, viewPortSize.Y, 1f, -1f);

            // NOTE: (+ degrees) rotates CCW and (- degrees) rotates CW
            var angleRadians = angle.ToRadians();

            // Invert angle to rotate CW instead of CCW
            angleRadians *= -1;

            var rotation = Matrix4x4.CreateRotationZ(angleRadians);
            var scaleMatrix = Matrix4x4.CreateScale(scaleX, scaleY, 1f);
            var positionMatrix = Matrix4x4.CreateTranslation(new Vector3(ndcX, ndcY, 0));

            return rotation * scaleMatrix * positionMatrix;
        }
    }
}
