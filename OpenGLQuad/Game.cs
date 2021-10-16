using System;
using System.Buffers;
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

namespace OpenGLQuad
{
    public class Game
    {
        // ReSharper disable once InconsistentNaming
        private static GL? GL;
        private static DebugProc? _debugCallback;
        private readonly IWindow _glWindow;
        private ShaderProgram? _shader;
        private Quad _quadA;
        private Quad _quadB;

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
            _glWindow.Update += OnUpdate;
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

            _quadA = new Quad(GL, _shader);
            _quadA.Position = new Vector2(200, 200);
            _quadA.Width = 100;
            _quadA.Height = 100;
            _quadA.Color = Color.Goldenrod;

            _quadB = new Quad(GL, _shader);
            _quadB.Position = new Vector2(265, 250);
            _quadB.Width = 40;
            _quadB.Height = 75;
            _quadB.Color = Color.CornflowerBlue;
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

            _quadA.Render();
             _quadB.Render();

            // Swap the back buffer with the front buffer
            _glWindow.SwapBuffers();
        }

        private void OnClose()
        {
            _quadA.Dispose();
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
