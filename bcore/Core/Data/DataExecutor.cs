using Lnksnk.Core.IO;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Lnksnk.Core.Data
{
    public class DataExecutor: IDisposable { 
        private DBConnection dBConnection;
        internal DbConnection dbcn;
        internal DbCommand dbcmd = null;
        private string lastquery = "";
        private string query = "";
        internal Dictionary<string, object> prms = null;
        internal List<string> prmslist = null;

        private static DataExecutor emptyDataExecutor = new DataExecutor(null, null);

        public static DataExecutor EmptyDataExecutor() {
            return emptyDataExecutor;
        }

        public DataExecutor(DBConnection dBConnection, DbConnection dbcn)
        {
            this.dbcn = dbcn;
            this.dBConnection = dBConnection;
        }

        public string Parenthises() {
            return this.dBConnection.Parenthises();
        }

        private Exception lastExecErr = null;

        private DataReader dataReader = null;
        
        public Exception Execute(string query,params object[] parameters)
        {
            this.lastExecErr = null;
            this.dataReader = null;
            this.query = query;
            return DBConnection.DataExecute(out lastExecErr, this.dBConnection, this.query, this,out this.dataReader, parameters: parameters);
        }

        public void WriteJSON(Object output, bool forceClose = false)
        {
            Newtonsoft.Json.JsonWriter jsonWrtr = null;
            if (output is System.IO.StreamWriter)
            {
                jsonWrtr = new Newtonsoft.Json.JsonTextWriter((System.IO.StreamWriter)output);
            }
            else if (output is System.IO.TextWriter)
            {
                jsonWrtr = new Newtonsoft.Json.JsonTextWriter((System.IO.TextWriter)output);
            }
            if (jsonWrtr != null)
            {
                Task.WaitAll(Task.Run(async () => {
                    await jsonWrtr.WriteStartObjectAsync();
                    await jsonWrtr.WritePropertyNameAsync("status");
                    if (this.lastExecErr == null)
                    {
                        await jsonWrtr.WriteValueAsync("executed");
                    } else
                    {
                        await jsonWrtr.WriteValueAsync("failed");
                    }
                    await jsonWrtr.WriteEndObjectAsync();
                    await jsonWrtr.FlushAsync();
                }));
                if (forceClose)
                {
                    if (output is System.IO.TextWriter)
                    {
                        ((System.IO.TextWriter)output).Close();
                    }
                    else if (output is System.IO.StreamWriter)
                    {
                        ((System.IO.StreamWriter)output).Close();
                    }
                }
            }
        }
        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (dbcmd != null)
                    {
                        try { dbcmd.Cancel(); } catch (Exception) { }
                        dbcmd = null;
                    }
                    if (dbcn != null)
                    {
                        try { dbcn.Close(); } catch (Exception) { }
                        dbcn = null;
                    }
                    if (this.dBConnection != null) {
                        this.dBConnection = null;
                    }
                }
                disposedValue = true;
            }
        }

         ~DataExecutor()
        {
           Dispose(!disposedValue);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}