using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lnksnk.Core.IO
{
    public abstract class BaseRW: IDisposable
    {
        private int bufferSize = 81920;
        public int BufferSize => this.bufferSize;

        public BaseRW(int bufferSize = 81920, Encoding encoding = null)
        {
            if (encoding == null)
            {
                this.encoding = System.Text.Encoding.UTF8;
            }
            else
            {
                this.encoding = encoding;
            }
            this.bufferSize = bufferSize;
            this.baseReader = new BaseReader(this);
            this.baseWriter = new BaseWriter(this);
        }

        private BaseReader baseReader = null;
        public TextReader Reader => this.baseReader;

        private BaseWriter baseWriter = null;

        internal abstract void WriteBuffer(char[] buffer, int index, int count);

        public TextWriter Writer => this.baseWriter;

        private Encoding encoding = null;
        internal Encoding Encoding { get { return this.encoding; } set { this.encoding = value; } }

        private string newline = "\r\n";
        internal string NewLine { get { return this.newline; } set { this.newline = value; } }

        internal abstract int ReadBuffer(char[] buffer, int index, int count);

        internal abstract void FlushBuffer();

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (this.baseWriter != null)
                    {
                        this.baseWriter.Close();
                        this.baseWriter = null;
                    }
                    if (this.baseReader != null)
                    {
                        this.baseReader.Close();
                        this.baseReader = null;
                    }
                }

                disposedValue = true;
            }
        }

        ~BaseRW()
        {
            Dispose(!this.disposedValue);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
