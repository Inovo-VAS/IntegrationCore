using System;
using System.Collections.Generic;
using System.Text;

namespace Lnksnk.Channeling
{
    public class Channel
    {
        private Direction direction = Direction.In;

        public Channel(Direction direction = Direction.In) {
            this.direction = direction;
        }
    }

    public enum Direction
    {
        In = 0,
        Out = 1,
        InOut = 2
    }
}
