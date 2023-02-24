using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lnksnk.Core.IO
{
    public class BufferRW : BaseRW, IDisposable
    {
        private List<char[]> buffers = new List<char[]>();
        private char[] wbuffer = null;
        private int wbufi = 0;

        private char[] rbuffer = null;
        private int rbufi = 0;

        private object lck = new object();

        public BufferRW(int bufferSize=81920,Encoding encoding=null):base(bufferSize,encoding) {
        }
        
        internal override int ReadBuffer(char[] buffer, int index, int count) {
            lock (this.lck) { 
            }
            return 0;
        }

        internal override void FlushBuffer() {
            
        }

        internal override void WriteBuffer(char[] buffer,int index,int count)
        {
            var wbfl = buffer == null ? 0 : (index + (buffer.Length - index) >= count ? count : (buffer.Length - index));
            if (wbfl > 0)
            {
                lock (this.lck)
                {
                    while (wbfl > index)
                    {
                        while (wbfl > index && this.wbufi < (this.wbuffer==null?(this.wbuffer=new char[this.BufferSize]):this.wbuffer).Length)
                        {
                            if ((wbfl - index) >= (this.wbuffer.Length - this.wbufi))
                            {
                                System.Array.Copy(buffer, index, this.wbuffer, this.wbufi, (this.wbuffer.Length - this.wbufi));
                                index += (this.wbuffer.Length - this.wbufi);
                                this.wbufi += (this.wbuffer.Length - this.wbufi);
                            }
                            else if ((wbfl - index) < (this.wbuffer.Length - this.wbufi))
                            {
                                System.Array.Copy(buffer, index, this.wbuffer, this.wbufi, (wbfl - index));
                                this.wbufi += (wbfl - index);
                                index += (wbfl - index);
                            }
                            if (this.wbuffer.Length == this.wbufi) {
                                this.buffers.Add(this.wbuffer);
                                this.wbufi = 0;
                                this.wbuffer = null;
                            }
                        }
                    }
                }
            }
        }
    }
}
