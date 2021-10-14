using System;

namespace OpenGLQuad
{
    public class Program
    {
        private static Game _game;

        public static void Main(string[] args)
        {
            _game = new Game();
            _game.Run();
        }
    }
}
