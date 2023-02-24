using System;
using System.Collections.Generic;
using System.Text;

namespace Lnksnk.Channeling
{
    public class ChannelsManager
    {
        private Dictionary<Direction,List<Channel>> channels = new Dictionary<Direction, List<Channel>>();
        private Dictionary<Direction, Dictionary<string, Channel>> directionChannels = new Dictionary<Direction, Dictionary<string, Channel>>();

        public void RegisterChannel(string alias, Direction direction,params object[] channelparams) { 
        }

        public void RegisterInChannel(string alias,params object[] channelparams)
        {
        }

        public void RegisterOutChannel(string alias,params object[] channelparams)
        {
            this.RegisterChannel(alias, direction: Direction.Out);
        }

        public void RegisterInOutChannel(string alias, params object[] channelparams)
        {
        }
    }
}
