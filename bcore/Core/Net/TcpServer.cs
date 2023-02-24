using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lnksnk.Core.Net
{
    class TcpServer
    {
        System.Net.Sockets.TcpListener tcpListener = null;
        private int port = 0;
        private string host = "";

        public ManualResetEvent allDone = new ManualResetEvent(false);

        TcpServer(int port,string host="0.0.0.0") {
            this.port = port;
            this.host = host;
            this.tcpListener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Parse(this.host), this.port);
        }

        public async Task StartListening() {
            await Task.Run(()=> {

                this.tcpListener.Start(15);
                while (true)
                {
                    this.allDone.Reset();
                    this.tcpListener.BeginAcceptTcpClient(new AsyncCallback(this.EndAcceptTcpCiient),this.tcpListener);

                }
            });
        }

        private void EndAcceptTcpCiient(IAsyncResult ar)
        {
            this.allDone.Set();
            var tcplstn = (System.Net.Sockets.TcpListener)ar.AsyncState;
            var tcpclnt = tcplstn.EndAcceptTcpClient(ar);


        }
    }
}
