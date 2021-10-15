using System;
using Silk.NET.OpenGL;
using Silk.NET.Vulkan;

namespace OpenGLQuad
{
    public class Quad : IDisposable
    {
        private readonly GL _gl;
        private readonly ShaderProgram _shader;
        private uint _vao; // Vertex Array Object
        private uint _vbo; // Vertex Buffer Object
        private uint _ebo; // Element Buffer Object
        // All of the vertices required to render a quad
        // 2 of the vertices are reused from the 2 triangles.  That is why
        // there is only 4 instead of 6
        private readonly float[] _vertices =
        {
            -0.5f,  0.5f, 0.0f, // Top Left Vert | Top Left Triangle
            -0.5f, -0.5f, 0.0f, // Bottom Left Vert | Top Left Triangle & Bottom Right Triangle
            0.5f,  0.5f, 0.0f, // Top Right Vert | Top Left Triangle & Bottom Right Triangle
            0.5f, -0.5f, 0.0f, // Bottom Right Vert | Bottom Right Triangle
        };

        // The references to the index locations in the _vertices array above
        private readonly uint[] _indices =
        {
            0u, 1u, 2u, // Top Left Triangle
            2u, 1u, 3u  // Bottom Left Triangle
        };

        public Quad(GL gl, ShaderProgram shader)
        {
            _gl = gl;
            _shader = shader;

            // Generate the VAO and VBO with only 1 object each
            _gl.GenVertexArrays(1, out _vao);
            _gl.GenBuffers(1, out _vbo);
            _gl.GenBuffers(1, out _ebo);

            SetupData();
        }

        public void UpdateData()
        {
            BindVAO();

            UpdateVertexData(_vertices);

            UpdateIndiceData(_indices);
        }

        public void Render()
        {
            // Tell OpenGL which Shader Program we want to use
            _shader?.Use();

            // Bind the VAO so OpenGL knows to use it
            BindVAO();

            unsafe
            {
                // Draw the triangle using the GL_TRIANGLES primitive
                _gl.DrawElements(PrimitiveType.Triangles, (uint)_indices.Length, DrawElementsType.UnsignedInt, (void*)0);
            }

            UnbindVAO();
        }

        private void UpdateVertexData(float[] vertices)
        {
            var vertData = new ReadOnlySpan<float>(vertices);

            BindVAO();
            BindVBO();

            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(sizeof(float) * _vertices.Length), vertData, BufferUsageARB.StaticDraw);

            UnbindVBO();
            UnbindVAO();
        }

        private void UpdateIndiceData(uint[] indices)
        {
            var indicesData = new ReadOnlySpan<uint>(indices);
            BindEBO();

            // Configure the Vertex Attribute so that OpenGL knows how to read the VBO
            _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(sizeof(uint) * _indices.Length), indicesData, BufferUsageARB.StaticDraw);

            UnbindEBO();
        }

        private void SetupVAO()
        {
            unsafe
            {
                _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
            }
        }

        private void SetupData()
        {
            BindVAO();
            BindVBO();

            var vertData = new ReadOnlySpan<float>(_vertices);
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(sizeof(float) * _vertices.Length), vertData, BufferUsageARB.StaticDraw);

            BindEBO();

            var indicesData = new ReadOnlySpan<uint>(_indices);
            // Configure the Vertex Attribute so that OpenGL knows how to read the VBO
            _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(sizeof(uint) * _indices.Length), indicesData, BufferUsageARB.StaticDraw);

            SetupVAO();

            EnableVAO();

            UnbindVBO();
            UnbindVAO();
            UnbindEBO();
        }

        private void EnableVAO() => _gl.EnableVertexAttribArray(0);

        private void BindVBO() => _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

        private void UnbindVBO() => _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0); // Unbind the VBO

        private void BindEBO() => _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);

        /// <summary>
        /// NOTE: Make sure to unbind AFTER you unbind the VAO.  This is because the EBO is stored
        /// inside of the VAO.  Unbinding the EBO before unbinding, (or without unbinding the VAO),
        /// you are telling OpenGL that you don't want your VAO to use the EBO.
        /// </summary>
        private void UnbindEBO() => _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);

        private void BindVAO() => _gl.BindVertexArray(_vao);

        private void UnbindVAO() => _gl.BindVertexArray(0); // Unbind the VAO

        public void Dispose()
        {
            _gl.DeleteVertexArray(_vao);
            _gl.DeleteBuffer(_vbo);
            _gl.DeleteBuffer(_ebo);
        }
    }
}
