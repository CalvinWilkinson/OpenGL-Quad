using System;
using System.Drawing;
using System.Numerics;
using Silk.NET.OpenGL;
using Silk.NET.Vulkan;
// ReSharper disable InconsistentNaming

namespace OpenGLQuad
{
    public class Quad : IDisposable
    {
        private const int SCREEN_WIDTH = 800;
        private const int SCREEN_HEIGHT = 600;
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

        private int _colorLocation;

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

        public Vector2 Position
        {
            get
            {
                var ndcWidth = NDCWidth();
                var ndcHeight = NDCHeight();
                var ndcX = NDCLeft() + (ndcWidth / 2f);
                var ndcY = NDCTop() - (ndcHeight / 2f); //Subtract to move down

                return ToPixel(ndcX, ndcY);
            }
            set
            {
                var ndcVector = ToNDC(value);
                var ndcHalfWidth = NDCWidth() / 2f;
                var ndcHalfHeight = NDCHeight() / 2f;

                var ndcLeft = ndcVector.X - ndcHalfWidth;
                var ndcRight = ndcVector.X + ndcHalfWidth;

                var ndcTop = ndcVector.Y + ndcHalfHeight;
                var ndcBottom = ndcVector.Y - ndcHalfHeight;

                // Update the vertices
                SetLeftSide(ndcLeft);
                SetRightSide(ndcRight);
                SetTop(ndcTop);
                SetBottom(ndcBottom);
                UpdateData();
            }
        }

        public int Width
        {
            get
            {
                var pixelLeft = NDCLeft().MapValue(-1f, 1f, 0f, SCREEN_WIDTH);
                var pixelRight = NDCRight().MapValue(-1f, 1f, 0f, SCREEN_WIDTH);

                return Math.Abs((int)pixelRight - (int)pixelLeft);
            }
            set
            {
                var ndcWidth = ((float)value).MapValue(0f, SCREEN_WIDTH, -1f, 1f);
                var ndcHalfWidth = ndcWidth / 2f;

                SetLeftSide(NDCLeft() - ndcHalfWidth);
                SetRightSide(NDCRight() + ndcHalfWidth);
                UpdateData();
            }
        }

        public int Height
        {
            get
            {
                var pixelTop = NDCTop().MapValue(1f, -1f, 0f, SCREEN_HEIGHT);
                var pixelBottom = NDCBottom().MapValue(1f, -1f, 0f, SCREEN_HEIGHT);

                return Math.Abs((int)pixelBottom - (int)pixelTop);
            }
            set
            {
                var ndcHeight = ((float)value).MapValue(0f, SCREEN_HEIGHT, -1f, 1f);
                var ndcHalfHeight = ndcHeight / 2f;

                SetTop(NDCTop() - ndcHalfHeight);
                SetBottom(NDCBottom() + ndcHalfHeight);
                UpdateData();
            }
        }

        public Color Color { get; set; } = Color.White;

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

            UpdateColorData(Color);

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

            _colorLocation = _gl.GetUniformLocation(_shader.Id, "u_color");

            UpdateColorData(Color.White);
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

        private float NDCLeft() => _vertices[0];

        private float NDCRight() => _vertices[6];

        private float NDCTop() => _vertices[1];

        private float NDCBottom() => _vertices[10];

        private float NDCWidth() => NDCRight() - NDCLeft();

        private float NDCHeight() => NDCBottom() - NDCTop();

        private void SetLeftSide(float ndcValue)
        {
            _vertices[0] = ndcValue;
            _vertices[3] = ndcValue;
        }

        private void SetRightSide(float ndcValue)
        {
            _vertices[6] = ndcValue;
            _vertices[9] = ndcValue;
        }

        private void SetTop(float ndcValue)
        {
            _vertices[1] = ndcValue;
            _vertices[7] = ndcValue;
        }

        private void SetBottom(float ndcValue)
        {
            _vertices[4] = ndcValue;
            _vertices[10] = ndcValue;
        }

        private void UpdateColorData(Color clr)
        {
            _shader?.Use();
            var red = ((float)clr.R).MapValue(0f, 255f, 0f, 1f);
            var green = ((float)clr.G).MapValue(0f, 255f, 0f, 1f);
            var blue = ((float)clr.B).MapValue(0f, 255f, 0f, 1f);
            var alpha = ((float)clr.A).MapValue(0f, 255f, 0f, 1f);

            _gl.Uniform4(_colorLocation, red, green, blue, alpha);
        }

        private Vector2 ToNDC(Vector2 pixelVector)
        {
            var ndcX = pixelVector.X.MapValue(0, SCREEN_WIDTH, -1f, 1f);
            var ndcY = pixelVector.Y.MapValue(0, SCREEN_HEIGHT, 1f, -1f);
            return new Vector2(ndcX, ndcY);
        }

        private Vector2 ToPixel(Vector2 ndcVector)
        {
            var pixelX = ndcVector.X.MapValue(-1f, 1f, 0, SCREEN_WIDTH);
            var pixelY = ndcVector.Y.MapValue(1f, -1f, 0, SCREEN_HEIGHT);
            return new Vector2(pixelX, pixelY);
        }

        private Vector2 ToPixel(float ndcX, float ndcY)
        {
            return ToPixel(new Vector2(ndcX, ndcY));
        }
    }
}
