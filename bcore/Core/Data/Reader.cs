using Lnksnk.Core.IO;
using Microsoft.AspNetCore.Server.IIS.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lnksnk.Core.Data
{
    public class Reader : System.IO.TextReader
    {
        DataExecutor dataExecutor = null;
        DataReader dataReader = null;

        private bool json = false;
        private bool csv = false;
        private string valdelim = "";
        private string txtpar = "";
        private string rowdelim = "";

        private BlockingCollection<char[]> blockingQueue = new BlockingCollection<char[]>();

        private BlockingReader blockingReader = null;

        private BlockingWriter blockingWriter = null;
        private object[] prms = null;
        private string query = "";
        public Reader(DataExecutor dataExecutor,string query,bool json=false,params object[] prms) { 
            this.dataExecutor = dataExecutor;
            this.json = json;
            this.prms = prms;
            this.query = query;
            this.blockingReader = new BlockingReader(this.blockingQueue);
            this.blockingWriter = new BlockingWriter(this.blockingQueue);
        }

        public Reader(DataReader dataReader, bool json = false,bool csv=false, string valdelim="", string txtpar="", string rowdelim="")
        {
            this.dataReader = dataReader;
            this.json = json;
            this.csv = csv;
            this.valdelim = valdelim;
            this.rowdelim = rowdelim;
            this.txtpar = txtpar;
            this.blockingReader = new BlockingReader(this.blockingQueue);
            this.blockingWriter = new BlockingWriter(this.blockingQueue);
        }

        private bool startReading = false;

        public override Task<int> ReadAsync(char[] buffer, int index, int count)
        {
            if (!startReading) {
                this.startReading = true;
                new Thread(()=>
                {
                    if (this.dataReader != null)
                    {
                        if (this.json)
                        {
                            this.dataReader.WriteJSON(this.blockingWriter, true);
                        }
                        else if (this.csv)
                        {
                            this.dataReader.WriteDVL(this.blockingWriter, this.valdelim, this.txtpar, this.rowdelim, true);
                        }
                    } else if (this.dataExecutor!=null)
                    {
                        if (this.json)
                        {
                            this.dataExecutor.Execute(this.query, parameters: this.prms);
                            this.dataExecutor.WriteJSON(this.blockingWriter, true);
                        }
                    }
                }).Start();
            }
            return  this.blockingReader.ReadAsync(buffer, index, count);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) {
                if (this.blockingWriter != null) {
                    this.blockingWriter.Dispose();
                    this.blockingWriter = null;
                }
                if (this.blockingReader != null) {
                    this.blockingReader.Dispose();
                    this.blockingReader = null;
                }
                if (this.blockingQueue != null)
                {
                    this.blockingQueue.Dispose();
                    this.blockingQueue = null;
                }
                if (this.dataReader != null) {
                    this.dataReader.Dispose();
                    this.dataReader = null;
                }
                if (this.dataExecutor != null) {
                    this.dataExecutor.Dispose();
                    this.dataExecutor = null;
                }
            }
        }
    }
}
