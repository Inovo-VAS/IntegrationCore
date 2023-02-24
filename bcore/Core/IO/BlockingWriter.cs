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
    public class BlockingWriter: TextWriter
    {
        private BlockingCollection<char[]> blckngbuffer = null;

        public BlockingWriter(BlockingCollection<char[]> blockngbuffer) {
            this.blckngbuffer = blockngbuffer;
        }

        public override Encoding Encoding => throw new NotImplementedException();

        private char[] outoutbuffer = new char[8192];
        private int outputbufi = 0;

        public override void Write(char[] buffer, int index, int count)
        {
            var bufmxl = buffer == null ? 0 : (index+(buffer.Length-index)>=count?count: (buffer.Length - index));
            var bufi = index;
            while (bufi < bufmxl) {
                if ((bufmxl - bufi) >= (this.outoutbuffer.Length - this.outputbufi)) {
                    Array.Copy(buffer, bufi, this.outoutbuffer, this.outputbufi, (this.outoutbuffer.Length - this.outputbufi));
                    bufi += (this.outoutbuffer.Length - this.outputbufi);
                    this.outputbufi += (this.outoutbuffer.Length - this.outputbufi);
                } else if ((bufmxl - bufi) < (this.outoutbuffer.Length - this.outputbufi))
                {
                    Array.Copy(buffer, bufi, this.outoutbuffer, this.outputbufi, (bufmxl - bufi));
                    this.outputbufi += (bufmxl - bufi);
                    bufi += (bufmxl - bufi);
                }
                if (this.outputbufi == this.outoutbuffer.Length) {
                    this.Flush();
                }
            }
        }

        public override void Write(char[] buffer)
        {
            if (buffer != null&&buffer.Length>0) {
                this.Write(buffer,0,buffer.Length);
            }
        }

        public override void Write(bool value)
        {
            this.Write(bool.TrueString);
        }

        private char[] singlechar = new char[1];
        public override void Write(char value)
        {
            this.singlechar[0] = value;
            this.Write(this.singlechar);
        }

        public override void Write(decimal value)
        {
            this.Write(value.ToString());
        }

        public override void Write(double value)
        {
            this.Write(value.ToString());
        }

        public override void Write(float value)
        {
            this.Write(value.ToString());
        }

        public override void Write(int value)
        {
            this.Write(value.ToString());
        }

        public override void Write(long value)
        {
            this.Write(value.ToString());
        }

        public override void Write(ReadOnlySpan<char> buffer)
        {
            if(buffer!=null&&!buffer.IsEmpty&&buffer.Length>0)
            this.Write(buffer.ToArray());
        }

        public override void Write(StringBuilder value)
        {
            if (value != null && value.Length > 0)
            {
                this.Write(value.ToString(0, value.Length));
            }
        }

        public override void Write(string value)
        {
            if (value != null && value.Length > 0) {
                this.Write(value.ToCharArray());
            }
        }

        public override void Write(uint value)
        {
            this.Write(value.ToString());
        }

        public override void Write(ulong value)
        {
            this.Write(value.ToString());
        }

        public async override Task WriteAsync(char[] buffer, int index, int count)
        {
            await Task.Run(()=>{
                this.Write(buffer, index, count);
            });
        }

        public async override Task WriteAsync(char value)
        {
            await Task.Run(() =>
            {
                this.Write(value);
            });
        }

        public  async override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Run(()=>
            {
                this.Write(buffer);
            },cancellationToken);
        }

        public async override Task WriteAsync(string value)
        {
            await Task.Run(() =>
            {
                this.Write(value);
            });
        }

        public async override Task WriteAsync(StringBuilder value, CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                this.Write(value);
            },cancellationToken);
        }

        public override void WriteLine()
        {
            this.Write(this.NewLine);
        }

        public override void WriteLine(bool value)
        {
            this.Write(value.ToString()+this.NewLine);
        }

        public override void WriteLine(char value)
        {
            this.Write(value+this.NewLine);
        }

        public override void WriteLine(char[] buffer, int index, int count)
        {
            this.Write(buffer, index, count);
            this.Write(this.NewLine);
        }

        public override void WriteLine(char[] buffer)
        {
            this.Write(buffer);
            this.Write(this.NewLine);
        }

        public override void WriteLine(decimal value)
        {
            this.WriteLine(value.ToString());
        }

        public override void WriteLine(double value)
        {
            this.WriteLine(value.ToString());
        }

        public override void WriteLine(float value)
        {
            this.WriteLine(value.ToString());
        }

        public override void WriteLine(int value)
        {
            this.WriteLine(value.ToString());
        }

        public override void WriteLine(long value)
        {
            this.WriteLine(value.ToString());
        }

        public override void WriteLine(ReadOnlySpan<char> buffer)
        {
            this.Write(buffer);
            this.WriteLine();
        }

        public override void WriteLine(string value)
        {
            this.Write(value);
            this.WriteLine();
        }

        public override void WriteLine(StringBuilder value)
        {
            this.WriteLine((value!=null&&value.Length>0)?value.ToString(0,value.Length):"");
        }

        public override void WriteLine(uint value)
        {
            this.WriteLine(value.ToString());
        }

        public override void WriteLine(ulong value)
        {
            this.WriteLine(value.ToString());
        }

        public async override Task WriteLineAsync()
        {
            await Task.Run(()=>{
                this.WriteLine();
            });
        }

        public async override Task WriteLineAsync(char value)
        {
            await Task.Run(() => {
                this.WriteLine(value);
            });
        }

        public async override Task WriteLineAsync(char[] buffer, int index, int count)
        {
            await Task.Run(() => {
                this.WriteLine(buffer,index,count);
            });
        }

        public async override Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Run(() => {
                this.WriteLine(buffer);
            },cancellationToken);
        }

        public async override Task WriteLineAsync(string value)
        {
            await Task.Run(() => {
                this.WriteLine(value);
            });
        }

        public async override Task WriteLineAsync(StringBuilder value, CancellationToken cancellationToken = default)
        {
            await Task.Run(() => {
                this.WriteLine(value);
            },cancellationToken);
        }

        public override void Flush()
        {
            if (this.outputbufi > 0) {
                var buftoout = this.outoutbuffer.AsSpan(0, this.outputbufi).ToArray();
                this.outputbufi = 0;
                while (this.blckngbuffer!=null&&!this.blckngbuffer.TryAdd(buftoout, 100)) ;
            }
        }

        public async override Task FlushAsync()
        {
            await Task.Run(() => {
                this.Flush();
            });
        }

        public async override ValueTask DisposeAsync()
        {
            await Task.Run(() => {
                this.Dispose();
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) {
                if (this.blckngbuffer != null) {
                    if (!this.blckngbuffer.IsAddingCompleted){
                        this.blckngbuffer.CompleteAdding();
                    }
                    this.blckngbuffer = null;
                }
            }
        }

        public override void Close()
        {
            if (this.blckngbuffer != null)
            {
                this.Flush();
                if (!this.blckngbuffer.IsAddingCompleted){
                    this.blckngbuffer.CompleteAdding();
                }
                this.blckngbuffer = null;
            }
        }
    }
}
