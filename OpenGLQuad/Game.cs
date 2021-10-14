using System;
using System.Buffers;
using System.Runtime.InteropServices;
using Silk.NET.GLFW;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
// ReSharper disable CommentTypo

namespace OpenGLQuad
{
    public class Game
    {
        // ReSharper disable once InconsistentNaming
        private static GL? GL;
        private static DebugProc? _debugCallback;
        private readonly IWindow _glWindow;
        private ShaderProgram? _shader;

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

        /*
            Left From Center X (- Value): ⬅
            Right From Center X (- Value): ➡
            Up From Center Y (+ Value): ⬆
            Up From Center Y (+ Value): ⬇

            Indice 0 (-0.5, 0.5)         Indice 2 (0.5, 0.5)
                           \             /
                            \           /
                             |--------/|
                             |       / |
                             |      /  |
                             |     /   |
                             |    *----|-------Center (0, 0)
                             |   /     |
                             |  /      |
                             | /       |
                             |/________|
                            /           \
                           /             \
            Indice 1 (-0.5, -0.5)         Indice 3 (0.5, -0.5)
         */

        private uint _vao; // Vertex Array Object
        private uint _vbo; // Vertex Buffer Object
        private uint _ebo; // Element Buffer Object
        private readonly Glfw _glfw;

        /// <summary>
        /// Creates a new instance of <see cref="Game"/>.
        /// </summary>
        public Game()
        {
            var options = WindowOptions.Default;
            var api = new GraphicsAPI(ContextAPI.OpenGL, new APIVersion(4, 4))
            {
                Profile = ContextProfile.Core
            };

            options.API = api;
            options.ShouldSwapAutomatically = false;
            options.Size = new Vector2D<int>(800, 600);
            options.Title = "LearnOpenGL with Silk.NET";
            options.Position = new Vector2D<int>(400, 400);
            _glWindow = Window.Create(options);

            _glWindow.Load += OnLoad;
            _glWindow.Render += OnRender;
            _glWindow.Closing += OnClose;
            _glWindow.Title = "Simple Quad";
        }

        /// <summary>
        /// Loads the game content.
        /// </summary>
        private unsafe void OnLoad()
        {
            // Must be called in the on load.  The OpenGL context must be created first
            // and that does not occur until the onload method has been invoked
            GL = GL.GetApi(_glWindow);
            SetupErrorCallback();

            _shader = new ShaderProgram(GL, "shader", "shader");

            // Generate the VAO and VBO with only 1 object each
            GL?.GenVertexArrays(1, out _vao);
            GL?.GenBuffers(1, out _vbo);
            GL?.GenBuffers(1, out _ebo);

            // Make the VAO the current Vertex Array Object by binding it
            GL?.BindVertexArray(_vao);

            // Bind the VBO specifying it's a GL_ARRAY_BUFFER
            var vertData = new ReadOnlySpan<float>(_vertices);
            GL?.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            GL?.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(sizeof(float) * _vertices.Length), vertData, BufferUsageARB.StaticDraw);

            // Bind and upload the indices data
            var indicesData = new ReadOnlySpan<uint>(_indices);
            GL?.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
            GL?.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(sizeof(uint) * _indices.Length), indicesData, BufferUsageARB.StaticDraw);

            // Configure the Vertex Attribute so that OpenGL knows how to read the VBO
            GL?.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);

            // Enable the Vertex Attribute so that OpenGL knows to use it
            GL?.EnableVertexAttribArray(0);

            // Bind both the VBO and VAO to 0 so that we don't accidentally modify the VAO and VBO we created
            GL?.BindBuffer(BufferTargetARB.ArrayBuffer, 0); // Unbind the VBO
            GL?.BindVertexArray(0); // Unbind the VAO

            // Unbind the EBO
            // NOTE: Make sure to unbind AFTER you unbind the VAO.  This is because the EBO is stored
            // inside of the VAO.  Unbinding the EBO before unbinding, (or without unbinding the VAO),
            // you are telling OpenGL that you don't want your VAO to use the EBO.
            GL?.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
        }

        private void OnRender(double obj)
        {
            // Specify the color of the background
            GL?.ClearColor(0.07f, 0.13f, 0.17f, 1.0f);

            // Clean the back buffer and assign the new color to it
            GL?.Clear(ClearBufferMask.ColorBufferBit);

            // Tell OpenGL which Shader Program we want to use
            _shader?.Use();

            // Bind the VAO so OpenGL knows to use it
            GL?.BindVertexArray(_vao);

            unsafe
            {
                // Draw the triangle using the GL_TRIANGLES primitive
                GL?.DrawElements(PrimitiveType.Triangles, (uint)_indices.Length, DrawElementsType.UnsignedInt, (void*)0);
            }

            // Swap the back buffer with the front buffer
            _glWindow.SwapBuffers();
        }

        private void OnClose()
        {
            GL?.DeleteVertexArray(_vao);
            GL?.DeleteBuffer(_vbo);
            GL?.DeleteBuffer(_ebo);
            _shader?.Dispose();
        }

        /// <summary>
        /// Runs the game.
        /// </summary>
        public void Run() => _glWindow.Run();

        /// <summary>
        /// Setup the callback to be invoked when OpenGL encounters an internal error.
        /// </summary>
        private void SetupErrorCallback()
        {
            if (_debugCallback != null) return;

            _debugCallback = DebugCallback;

            /*NOTE:
             * This is here to help prevent an issue with an obscure System.ExecutionException from occurring.
             * The garbage collector performs a collect on the delegate passed into GL?.DebugMesageCallback()
             * without the native system knowing about it which causes this exception. The GC.KeepAlive()
             * method tells the garbage collector to not collect the delegate to prevent this from happening.
             */
            GC.KeepAlive(_debugCallback);

            GL?.DebugMessageCallback(_debugCallback, Marshal.StringToHGlobalAnsi(string.Empty));
        }

        /// <summary>
        /// Throws an exception when error OpenGL errors occur.
        /// </summary>
        /// <exception cref="Exception">The OpenGL message as an exception.</exception>
        private void DebugCallback(GLEnum source, GLEnum type, int id, GLEnum severity, int length, nint message, nint userParam)
        {
            var errorMessage = Marshal.PtrToStringAnsi(message);

            errorMessage += $"\n\tSrc: {source}";
            errorMessage += $"\n\tType: {type}";
            errorMessage += $"\n\tID: {id}";
            errorMessage += $"\n\tSeverity: {severity}";
            errorMessage += $"\n\tLength: {length}";
            errorMessage += $"\n\tUser Param: {Marshal.PtrToStringAnsi(userParam)}";

                throw new Exception(errorMessage);
            if (severity != GLEnum.DebugSeverityNotification)
            {
            }
        }
    }
}
