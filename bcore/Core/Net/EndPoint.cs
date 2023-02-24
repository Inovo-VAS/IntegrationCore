using System;
using System.Collections.Generic;
using System.Text;

namespace Lnksnk.Core.Net
{
    public class EndPoint
    {
        private int port = 0;
        public int Port { get => this.port; set { this.port = value; } }

        private bool ssl = false;
        public bool Ssl { get => this.ssl; set { this.ssl = value; } }

        private string certificatefile = "";
        public string Certificatefile { get => this.certificatefile; set { this.certificatefile = value; } }
    }
}
