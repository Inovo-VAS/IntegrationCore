using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Bcoring.ES6.Expressions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lnksnk.Core.IO
{
    public class BlockingReader : TextReader
    {
        private BlockingCollection<char[]> blckngbuffer = null;

        public BlockingReader(BlockingCollection<char[]> blockingbuffer) {
            this.blckngbuffer = blockingbuffer;
        }

        private char[] current = null;
        private int currenti = 0;
        private int currentl = 0;

        public override int Read(char[] buffer, int index, int count)
        {
            return this.ReadAsync(buffer, index, count).Result;
        }

        public async override Task<int> ReadAsync(char[] buffer, int index, int count)
        {
            return await Task<int>.Run(() => {
                var bufmxl = buffer == null ? 0 : (index + (buffer.Length - index) >= count ? count : ((buffer.Length - index)));
                var bufi = index;
                var bufl = 0;
                while (bufi < bufmxl) {
                    if (this.currentl == 0 || (this.currentl > 0 && this.currenti == this.currentl)) {
                        this.currenti = 0;
                        this.currentl = 0;
                        this.current = null;
                        while (!this.blckngbuffer.TryTake(out this.current,100)) {
                            if (this.blckngbuffer.IsCompleted) {
                                break;
                            }
                        }
                        if (this.current != null) {
                            this.currentl = this.current.Length;
                        }
                        if (this.currentl == 0) {
                            break;
                        }
                    }
                    if ((bufmxl - bufi) >= (this.currentl - this.currenti))
                    {
                        Array.Copy(this.current, this.currenti, buffer, bufi, (this.currentl - this.currenti));
                        bufl += (this.currentl - this.currenti);
                        bufi += (this.currentl - this.currenti);
                        this.currenti += (this.currentl - this.currenti);
                    }
                    else if ((bufmxl - bufi) < (this.currentl - this.currenti)) {
                        Array.Copy(this.current, this.currenti, buffer, bufi, (bufmxl - bufi));
                        bufl += (bufmxl - bufi);
                        this.currenti += (bufmxl - bufi);
                        bufi += (bufmxl - bufi);
                    }
                }
                return bufl;
            });
        }

        public override string ReadToEnd()
        {
            return this.ReadToEndAsync().Result;
        }

        public async override Task<string> ReadToEndAsync()
        {
            var s = "";
            var chrs = new char[81920];
            var chrsl = 0;
            while ((chrsl = await this.ReadAsync(chrs, 0, chrs.Length)) > 0) {
                s += chrs[0..chrsl];
            }
            return s;
            
        }
    }
}
