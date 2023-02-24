using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Lnksnk.Core.IO
{
    public class BlockingRW : IDisposable
    {
        private BlockingCollection<char[]> blockingbuffer = null;

        private BlockingReader blockingReader = null;

        private BlockingWriter blockingWriter = null;
        private bool disposedValue;

        public bool IsCompleted => this.blockingbuffer == null ? true : this.blockingbuffer.IsCompleted;

        public int Count => this.blockingbuffer == null ? 0 : this.blockingbuffer.Count;

        public bool IsAddingCompleted => this.blockingbuffer == null ? false : this.blockingbuffer.IsAddingCompleted;
        private bool disposeBuffer = false;
        public BlockingRW(BlockingCollection<char[]> blockingbuffer,bool disposeBuffer=false) {
            this.blockingbuffer = blockingbuffer;
            this.disposeBuffer = disposeBuffer;
            this.blockingReader = new BlockingReader(this.blockingbuffer);
            this.blockingWriter = new BlockingWriter(this.blockingbuffer);
        }

        public BlockingReader Reader => this.blockingReader;

        public BlockingWriter Writer => this.blockingWriter;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (this.blockingWriter != null) {
                        this.blockingWriter.Close();
                        this.blockingWriter = null;
                    }
                    if (this.blockingReader != null)
                    {
                        this.blockingReader.Close();
                        this.blockingReader = null;
                    }
                    if (this.blockingbuffer != null) {
                        if (this.disposeBuffer) {
                            this.blockingbuffer.Dispose();
                        }
                        this.blockingbuffer = null;
                    }
                }

                disposedValue = true;
            }
        }

        ~BlockingRW()
        {
             Dispose(disposing: !this.disposedValue);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
