using System;
using System.Collections.Generic;
using System.Text;

namespace Lnksnk.Core.Net
{
    class TcpClient
    {
        private System.Net.Sockets.TcpClient tcpClient = null;
        private int port = 0;
        private string host = "";
        private System.Net.Sockets.NetworkStream networkStream = null;

        private TcpServer tcpServer = null;

        TcpClient(int port,string host):this(new System.Net.Sockets.TcpClient()) {
            this.tcpClient.SendBufferSize = 4096;
            this.tcpClient.ReceiveBufferSize = 4096;
        }

        TcpClient(System.Net.Sockets.TcpClient tcpClient, TcpServer tcpServer = null) {
            this.tcpClient = tcpClient;
            this.tcpServer = tcpServer;
        }

        public async void Connect(int timeout)
        {
            tcpClient.NoDelay = true;
            await tcpClient.ConnectAsync(this.host, this.port);
            this.networkStream = tcpClient.GetStream();
        }

        public async System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count) {
            return await this.networkStream.ReadAsync(buffer, offset, count);
        }

        public async System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count) {
            await this.networkStream.WriteAsync(buffer, offset, count);
        }
    }
}
