using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Lnksnk.Core.IO
{
    class BaseReader: TextReader
    {
        private BaseRW baseRW;

        internal BaseReader(BaseRW baseRW)
        {
            this.baseRW = baseRW;
        }

        private char[] chr = new char[1];
        public override int Read()
        {
            if (this.Read(this.chr, 0, 1) > 0)
            {
                return this.chr[0];
            }
            return -1;
        }

        public override string ReadLine()
        {
            return this.ReadLineAsync().Result;
        }

        public async override Task<string> ReadLineAsync()
        {
            var s = "";

            await Task.Run(() => {
                var ch = new char[1];
                var prvc = (char)0;
                while ((this.readBuffer(ch, 0, 1)) > 0)
                {
                    if (ch[0] == 10)
                    {
                        break;
                    }
                    else
                    {
                        if (ch[0] == 13) continue;
                        if (prvc == 13)
                        {
                            s += prvc;
                        }
                        s += ch;
                    }
                    prvc = ch[0];
                }
            });

            return s;
        }

        public override string ReadToEnd()
        {
            return this.ReadToEndAsync().Result;
        }

        public async override Task<string> ReadToEndAsync()
        {
            var s = "";
            await Task.Run(() => {
                var ch = new char[8192];
                var chl = 0;
                while ((chl = this.readBuffer(ch, 0, ch.Length)) > 0)
                {
                    s += ch.AsSpan(0, chl).ToArray();
                }
            });
            return s;
        }

        public override int Read(char[] buffer, int index, int count)
        {
            return this.ReadAsync(buffer, index, count).Result;
        }

        public override async Task<int> ReadAsync(char[] buffer, int index, int count)
        {
            return await Task.Run(() => {
                return this.readBuffer(buffer, index, count);
            });
        }

        private int readBuffer(char[] buffer, int index, int count)
        {
            return this.baseRW.ReadBuffer(buffer, index, count);
        }
    }
}
