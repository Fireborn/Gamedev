using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace LevelEditor
{
    class GameConsole
    {
        string[] _consoleTextLines;
        SpriteFont _consoleFont;
        int characterWidth = 100;

        public GameConsole(ContentManager content)
        {
            // Magic number, this really just depends on the font...
            _consoleTextLines = new string[100];

            _consoleFont = content.Load<SpriteFont>("DebugFont");
        }

        public void Show()
        {

        }

        public void Hide()
        {

        }

        public void Write(string newConsoleText)
        {
            while (newConsoleText.Length >= characterWidth)
            {
                string line = newConsoleText.Substring(0, characterWidth);
                newConsoleText = newConsoleText.Substring(characterWidth);

                Array.Copy(_consoleTextLines, 0, _consoleTextLines, 1, characterWidth - 1);
                _consoleTextLines[0] = line;
            }

            Array.Copy(_consoleTextLines, 0, _consoleTextLines, 1, characterWidth - 1);
            _consoleTextLines[0] = newConsoleText;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            Vector2 rasterPosition = Vector2.Zero;
            
            spriteBatch.Begin();

            for (int lineIndex = 0; lineIndex < _consoleTextLines.Length; ++lineIndex)
            {
                if(_consoleTextLines[lineIndex] != null)
                {
                    spriteBatch.DrawString(_consoleFont, _consoleTextLines[lineIndex], rasterPosition, Color.Black);
                }
                rasterPosition = rasterPosition + new Vector2(0, 15);
            }

            spriteBatch.End();
        }
    }
}
