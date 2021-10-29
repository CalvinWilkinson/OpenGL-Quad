using System;
using System.Buffers;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.GLFW;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
// ReSharper disable CommentTypo
/*
    Left From Center X (- Value): ⬅
    Right From Center X (- Value): ➡
    Up From Center Y (+ Value): ⬆
    Up From Center Y (+ Value): ⬇

            Indice 0 (-1.0, 1.0)          Indice 2 & 3 (1.0, 1.0)
                           \             /
                            \           /
                             |--------/|
                             |       / |
                             |      /  |
                             |     /   |
                             |    *----|-------Local Center (0, 0)
                             |   /     |
                             |  /      |
                             | /       |
                             |/________|
                            /           \
                           /             \
       Indice 1 & 4 (-1.0, -1.0)          Indice 5 (1.0, -1.0)
 */

namespace OpenGLQuad
{
    public class Game
    {
        private const uint BATCH_SIZE = 10u;
        private const int SCREEN_WIDTH = 1600;
        private const int SCREEN_HEIGHT = 1600;
        private static GL? GL;
        private static DebugProc? _debugCallback;
        private readonly IWindow _glWindow;
        private ShaderProgram? _shader;
        private GPUBuffer _gpuBuffer;
        private SpriteBatch _spriteBatch;
        private List<Rectangle> _rects = new();

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
            options.Size = new Vector2D<int>(SCREEN_WIDTH, SCREEN_HEIGHT);
            options.Title = "LearnOpenGL with Silk.NET";
            _glWindow = Window.Create(options);

            _glWindow.Load += OnLoad;
            _glWindow.Update += OnUpdate;
            _glWindow.Render += OnRender;
            _glWindow.Closing += OnClose;
            _glWindow.Title = "Simple GPUBuffer";
        }

        /// <summary>
        /// Loads the game content.
        /// </summary>
        private void OnLoad()
        {
            // Must be called in the on load.  The OpenGL context must be created first
            // and that does not occur until the onload method has been invoked
            GL = GL.GetApi(_glWindow);
            SetupErrorCallback();

            _glWindow.Position = new Vector2D<int>(400, 300);

            _shader = new ShaderProgram(GL, "shader", "shader", BATCH_SIZE);
            _gpuBuffer = new GPUBuffer(GL, _shader.Id, BATCH_SIZE);

            for (var i = 0; i < 1000; i++)
            {
                _rects.Add(GenerateRandomRect());
            }

            _spriteBatch = new SpriteBatch(GL, _gpuBuffer, _shader, BATCH_SIZE);
        }

        private Rectangle GenerateRandomRect()
        {
            var random = new Random();

            var red = random.Next(0, 255);
            var green = random.Next(0, 255);
            var blue = random.Next(0, 255);

            var width = (uint) random.Next(10, 150);
            var height = (uint) random.Next(10, 150);
            var halfWidth = width / 2.0f;
            var halfHeight = height / 2.0f;

            var x = random.Next((int)halfWidth, _glWindow.Size.X - (int)halfWidth);
            var y = random.Next((int)halfHeight, _glWindow.Size.Y - (int)halfHeight);

            return new Rectangle()
            {
                Position = new Vector2(x, y),
                Width = (uint)random.Next(10, 150),
                Height = (uint)random.Next(10, 150),
                Color = Color.FromArgb(255, red, green, blue)
            };
        }

        private void OnUpdate(double obj)
        {

        }

        private void OnRender(double obj)
        {
            // Specify the color of the background
            GL?.ClearColor(0.07f, 0.13f, 0.17f, 1.0f);

            // Clean the back buffer and assign the new color to it
            GL?.Clear(ClearBufferMask.ColorBufferBit);

            _spriteBatch.Begin();

            foreach (var rect in _rects)
            {
                _spriteBatch.RenderRectangle(rect);
            }

            _spriteBatch.End();

            // Swap the back buffer with the front buffer
            _glWindow.SwapBuffers();
        }

        private void OnClose()
        {
            _gpuBuffer.Dispose();
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

            if (severity != GLEnum.DebugSeverityNotification)
            {
                throw new Exception(errorMessage);
            }
        }
    }
}
