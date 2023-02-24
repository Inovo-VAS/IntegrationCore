using Lnksnk.Core.IO;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lnksnk.Core.Net.Web
{
    public delegate ValueTask<FlushResult> WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default);
    public class ResourceBase : IDisposable
    {
        BaseHandler basehndlr = null;

        private long readLength = 0;

        private CancellationTokenSource Cancellation = null;

        public long ReadLength { get { return this.readLength; } }

        protected RequestHandler RequestHandler {
            get { return  this.basehndlr is RequestHandler?(RequestHandler)this.basehndlr:null; }
        }
        private ResourceBase resourceBaseRef = null;

        private Lnksnk.Core.IO.ActiveReader strmrdr = null;
         
        protected Lnksnk.Core.IO.ActiveReader StreamReader { get { return this.strmrdr; } set { this.strmrdr = value; } }

        private System.IO.BinaryReader binstrmrdr = null;
        protected System.IO.BinaryReader BinaryReader { get { return this.binstrmrdr; } set { this.binstrmrdr = value; } }

        public long ResourceLength => this.binstrmrdr == null ? 0 : this.binstrmrdr.BaseStream.Length;

        private bool disposedValue;

        public ResourceBase(BaseHandler basehndlr) {
            this.basehndlr = basehndlr;
            this.resourceBaseRef = this;
        }

        public Action DoneReading = null;

        public Action DoneReadingNothing = null;

        public virtual void Print(params object[] ss)
        {
            if (this.strmrdr != null)
            {
                this.strmrdr.Print(ss: ss);
            }
        }
        public virtual void Println(params object[] ss)
        {
            if (this.strmrdr != null)
            {
                this.strmrdr.Println(ss: ss);
            }
        }

        private byte[] data = new byte[8192];
        private int datal = 0;

        private char[] chardata = new char[8192];
        private int chardatal = 0;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (this.Cancellation!=null)
                    {
                        if (this.Cancellation.Token.CanBeCanceled) {
                            this.Cancellation.Cancel();
                        }
                        this.Cancellation=null;
                    }
                    this.DisposingResourcePath();
                    if (this.binstrmrdr != null)
                    {
                        this.binstrmrdr.Close();
                        this.binstrmrdr = null;
                    }
                    if (this.strmrdr != null)
                    {
                        this.strmrdr.Close();
                        this.strmrdr = null;
                    }
                }

                disposedValue = true;
            }
        }

        public virtual void DisposingResourcePath() { }

        public virtual async Task<bool> WriteAsync(WriteAsync writeAsync, bool all = false,bool binread=false)
        {
            if (binread)
            {
                while (true)
                {
                    var rdbts = await this.ReadByteAsync();
                    if (rdbts.Length > 0)
                    {
                        foreach (var rdbt in rdbts)
                        {
                            if (this.Cancellation == null)
                            {
                                Cancellation = new CancellationTokenSource(1000);
                            }
                            var x = await writeAsync(rdbt, Cancellation.Token);
                            if (!x.IsCompleted)
                            {
                                if (x.IsCanceled)
                                {
                                    return false;
                                }
                            }
                        }
                        if (!all)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else
            {   
                while (true)
                {
                    var rdbts = await this.ReadCharAsync();
                    if (rdbts.Length > 0)
                    {
                        foreach (var rdbt in rdbts)
                        {
                            var x = await writeAsync(Encoding.UTF8.GetBytes(rdbt.ToArray()));
                            if (!x.IsCompleted)
                            {
                                if (x.IsCanceled)
                                {
                                    return false;
                                }
                                //Thread.Sleep(5);
                            }
                        }
                        if (!all)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        public Interrupting InterruptAction = null;

        public virtual void DoneReadingStream(Exception e) { }

        public void AddNextReadingSource(params object[] readingsource) {
            if (this.StreamReader == null)
            {
                this.strmrdr = new ActiveReader(true, this.RequestHandler != null ? this.RequestHandler.activeMap : null, this.InterruptAction) {
                    SourceFinder = basehndlr
                };
            }
            this.strmrdr.AddReadingSource(readingSource: readingsource);
        }

        public virtual async Task<ReadOnlySequence<Char>> ReadCharAsync() {
            if (this.strmrdr != null)
            {
                try
                {
                    if ((this.chardatal = await this.strmrdr.ReadAsync(this.chardata, 0, this.chardata.Length)) == 0)
                    { 
                        this.strmrdr.Close();
                        this.strmrdr = null;
                        this.DoneReadingStream(null);
                    }
                    else
                    {
                        this.readLength += this.chardatal;
                        if (this.DoneReading != null)
                        {
                            this.DoneReading();
                            this.DoneReading = null;
                            this.DoneReadingNothing = null;
                        }
                        this.RequestHandler.IncWrittenContent(this.chardatal);
                        return new ReadOnlySequence<char>(this.chardata, 0, this.chardatal);
                    }
                }
                catch (Exception e)
                {
                    this.strmrdr.Close();
                    this.strmrdr = null;
                    this.DoneReadingStream(e);
                }
            }
            if (this.DoneReading != null)
            {
                this.DoneReading = null;
                if (this.DoneReadingNothing != null)
                {
                    this.DoneReadingNothing();
                    this.DoneReadingNothing = null;
                }
            }
            return new ReadOnlySequence<char>();
        }

        public virtual void DoneReadingBinary(Exception e) { }

        public virtual async Task<ReadOnlySequence<Byte>> ReadByteAsync()
        {
            if (this.BinaryReader != null)
            {
                if (this.basehndlr!=null&&this.basehndlr.startOffset>-1)
                {
                    if (this.ResourceLength > this.basehndlr.startOffset)
                    {
                        this.BinaryReader.BaseStream.Position = this.basehndlr.startOffset;
                    }
                    this.basehndlr.startOffset = -1;
                }
                try
                {
                    if ((this.datal = await Task<int>.Run(() => { return this.BinaryReader.Read(this.data, 0, this.data.Length); })) == 0)
                    {
                        this.DoneReadingBinary(null);
                    }
                    else if (this.datal > 0)
                    {
                        this.readLength += this.datal;
                        if (this.DoneReading != null)
                        {
                            this.DoneReading();
                            this.DoneReading = null;
                            this.DoneReadingNothing = null;
                        }
                        this.RequestHandler.IncWrittenContent(this.datal);
                        return new ReadOnlySequence<byte>(this.data, 0, this.datal);
                    }
                }
                catch (Exception e)
                {
                    this.binstrmrdr.Close();
                    this.binstrmrdr = null;
                    this.DoneReadingBinary(e);
                }
            }

            if (this.DoneReading != null)
            {
                this.DoneReading = null;
                if (this.DoneReadingNothing != null)
                {
                    this.DoneReadingNothing();
                    this.DoneReadingNothing = null;
                }
            }
            return new ReadOnlySequence<byte>();
        }

        ~ResourceBase()
        {
            if (this.chardata != null)
            {
                this.chardata = null;
            }
            if (this.data != null)
            {
                this.data = null;
            }
            if (this.basehndlr != null)
            {
                this.basehndlr = null;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
