using System;
using System.Collections.Generic;
using System.Text;

namespace Lnksnk.Environment
{
    public enum EndpointType { 
        Local,
        Remote,
        DBMS
    }

    class EndPoint
    {
        public static EndPoint LocalEndPoint(string localPath) {
            return null;
        }

        public static EndPoint RemoteEndpoint(string remotepath)
        {
            return null;
        }
    }

}
