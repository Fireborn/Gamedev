using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FantasyRpgXna4
{
    class Player
    {
        public Player(Vector3 position, Model visualModel)
        {
            Position = position;
            VisualModel = visualModel;
        }

        public Vector3 Position
        {
            get;
            set;
        }

        public Model VisualModel
        {
            get;
            set;
        }
    }
}
