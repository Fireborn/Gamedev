using System;

namespace LevelEditor
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (LevelEditor game = new LevelEditor())
            {
                game.Run();
            }
        }
    }
#endif
}

