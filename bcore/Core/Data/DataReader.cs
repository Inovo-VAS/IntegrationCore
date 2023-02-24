using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lnksnk.Core.Data
{
    public class DataReader : IEnumerable<DataRecord>, IEnumerator<DataRecord>
    {
        private DBConnection dBConnection;
        private DbConnection dbcn;
        private DbCommand dbcmd;

        private static DataReader emptyDataReader = new DataReader(null, null, null, null);

        public static DataReader EmptyDataReader() {
            return emptyDataReader;
        }

        internal object GetData(string key)
        {
            if (this.columns != null && this.columns.IndexOf(key) > -1)
            {
                return this.recData[this.columns.IndexOf(key)];
            }
            return null;
        }

        private DbDataReader dbrdr;

        public DataReader(DBConnection dBConnection, DbConnection dbcn, DbCommand dbcmd, DbDataReader dbrdr)
        {
            this.dBConnection = dBConnection;
            this.dbcn = dbcn;
            this.dbcmd = dbcmd;
            this.dbrdr = dbrdr;
            try
            {
                if (dbrdr != null)
                {
                    if ((this.fieldcnt = this.dbrdr.FieldCount) > 0)
                    {
                        this.columns = new List<string>();
                        this.coltypes = new Type[this.fieldcnt];
                        var coli = 0;
                        while (coli < this.fieldcnt)
                        {
                            this.columns.Add(this.dbrdr.GetName(coli++));
                            this.coltypes[coli - 1] = this.dbrdr.GetFieldType(coli - 1);
                        }
                    }
                }
            }
            catch (Exception)
            {
                this.Dispose();
            }
        }

        private System.IO.StreamReader sin = null;
        private JsonReader jsin = null;
        private bool json = false;
        private bool csv = false;
        private string csvcoldelim = ",";
        private string csvrowdelim = "\r\n";
        private bool csvcolheaders = false;

        public DataReader(System.IO.Stream sin, bool json = false, bool csv = false, string csvcoldelim = ",", string csvrowdelim = "\r\n", bool csvcolheaders = true, string[] csvcoltypes = null, string[] csvcolumns = null):this(new StreamReader(sin),json,csv,csvcoldelim,csvrowdelim,csvcolheaders,csvcoltypes,csvcolumns) {

        }

        public DataReader(System.IO.StreamReader sin, bool json = false, bool csv = false, string csvcoldelim = ",", string csvrowdelim = "\r\n", bool csvcolheaders = true, string[] csvcoltypes = null, string[] csvcolumns = null)
        {
            if ((this.sin = sin) != null)
            {
                if (this.json = json)
                {
                    this.jsin = new JsonTextReader(this.sin);
                }
                else if (this.csv = csv)
                {
                    this.csvcoldelim = csvcoldelim;
                    this.csvrowdelim = csvcoldelim;
                }
                try
                {
                    if (this.json)
                    {
                        this.columns = new List<string>();
                        var cltypes = new List<Type>();
                        var coli = 0;

                        Task.WaitAll(Task.Run(async () => { 
                            while(await this.jsin.ReadAsync())
                            {
                                if (this.jsin.Depth == 0)
                                {
                                    if (this.jsin.TokenType == JsonToken.StartArray)
                                    {
                                        continue;
                                    }
                                    else if (this.jsin.TokenType == JsonToken.EndArray)
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                } else if (this.jsin.Depth == 1)
                                {
                                    if (this.jsin.TokenType == JsonToken.StartArray)
                                    {
                                        continue;
                                    }
                                    else if (this.jsin.TokenType == JsonToken.EndArray)
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                } else if (this.jsin.Depth == 2)
                                {
                                    if (this.jsin.TokenType == JsonToken.StartObject)
                                    {
                                        continue;
                                    }
                                    else if (this.jsin.TokenType == JsonToken.EndObject)
                                    {
                                    }
                                    else
                                    {
                                        break;
                                    }
                                } else if (this.jsin.Depth == 3)
                                {
                                    if (this.jsin.TokenType == JsonToken.PropertyName)
                                    {
                                        this.fieldcnt++;
                                        this.columns.Add((string)this.jsin.Value);
                                    }
                                    else if (this.jsin.TokenType == JsonToken.String)
                                    {
                                        var cltpy = (string)this.jsin.Value;
                                        var t = Type.GetType("System." + cltpy);
                                        if (t != null)
                                        {
                                            cltypes.Add(t);
                                        } else
                                        {
                                            cltypes.Add(typeof(string));
                                        }
                                    }
                                }
                            }
                        }));

                        this.coltypes = new Type[this.fieldcnt];
                        if (cltypes.Count==this.fieldcnt)
                        {
                            this.recData = new object[cltypes.Count];
                            System.Array.Copy(cltypes.ToArray(), this.coltypes, this.fieldcnt);
                        }
                        cltypes.Clear();
                    }
                }
                catch (Exception)
                {
                    this.Dispose();
                }
            }
        }

        public object[] Data => this.recData;

        public string[] Columns => this.columns==null?null:this.columns.ToArray();

        internal bool NextRec()
        {
            try
            {
                if (this.sin != null)
                {
                    var canNext = false;
                    if (this.json&&this.jsin!=null)
                    {
                        Task.WaitAll(Task.Run(async ()=> {
                            int cli = 0;
                            if (this.jsin.Depth > 0)
                            {
                                while (await this.jsin.ReadAsync())
                                {
                                    if (this.jsin.Depth==1)
                                    {
                                        if (this.jsin.TokenType == JsonToken.StartArray)
                                        {
                                            cli = 0;
                                            continue;
                                        }
                                        else if (this.jsin.TokenType == JsonToken.EndArray)
                                        {
                                            canNext = this.fieldcnt>0 && cli == this.fieldcnt;
                                            break;
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    } else if (this.jsin.Depth == 2)
                                    {
                                        if (this.jsin.TokenType == JsonToken.StartArray)
                                        {
                                            cli = 0;
                                            continue;
                                        }
                                        else if (this.jsin.TokenType == JsonToken.EndArray)
                                        {
                                            canNext = this.fieldcnt > 0 && cli == this.fieldcnt;
                                            break;
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                    else if (this.jsin.Depth==3)
                                    {
                                        if (this.jsin.TokenType!=JsonToken.StartArray&& this.jsin.TokenType != JsonToken.EndArray&& this.jsin.TokenType != JsonToken.StartObject&& this.jsin.TokenType != JsonToken.EndObject&&this.jsin.TokenType!=JsonToken.PropertyName)
                                        {
                                            if(cli<this.fieldcnt)
                                            {
                                                var t = this.coltypes[cli++];
                                                var v = this.jsin.Value;
                                                if (v == null)
                                                {
                                                    this.recData[cli - 1] = null;
                                                }
                                                else
                                                {
                                                    if (t == typeof(string))
                                                    {
                                                        this.recData[cli - 1] = (string)v;
                                                    }
                                                    else if (t == typeof(int) || t == typeof(long))
                                                    {
                                                        if (v is int)
                                                        {
                                                            this.recData[cli - 1] = (int)v;
                                                        }
                                                        else if (v is long)
                                                        {
                                                            this.recData[cli - 1] = (long)v;
                                                        }
                                                    }
                                                    else if (t == typeof(float) || t == typeof(double) || t == typeof(decimal))
                                                    {
                                                        if (v is float)
                                                        {
                                                            this.recData[cli - 1] = (float)v;
                                                        }
                                                        else if (v is double)
                                                        {
                                                            this.recData[cli - 1] = (double)v;
                                                        }
                                                        else
                                                        {
                                                            this.recData[cli - 1] = (decimal)v;
                                                        }
                                                    }
                                                    else if (t == typeof(DateTime))
                                                    {
                                                        this.recData[cli - 1] = (DateTime)v;
                                                    }
                                                }
                                            }
                                        } else
                                        {
                                            break;
                                        }
                                    }
                                }
                            }
                        }));
                    }
                    return canNext;
                }
                else if (this.dbrdr!=null&&this.dbrdr.Read())
                {
                    if ((((this.fieldcnt > 0)? (this.recData == null)?(this.recData = new object[this.fieldcnt]) :this.recData : null) != null) && this.fieldcnt > 0 && this.recData.Length == this.fieldcnt)
                    {
                        this.dbrdr.GetValues(this.recData);
                        for (var ci = 0; ci < this.recData.Length; ci++) {
                            if (this.dbrdr.IsDBNull(ci))
                            {
                                this.recData[ci] = null;
                            }
                            else {
                                this.recData[ci] = this.dbrdr.GetValue(ci);
                            }
                        }
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private Object[] recData = null;
        private List<string> columns;
        private Type[] coltypes;

        private int fieldcnt = 0;
        internal int FieldCount()
        {
            return this.fieldcnt;
        }

        private DataRecord dataRecord = null;

        public DataRecord Record => dataRecord;
        public DataRecord Current => dataRecord;

        object IEnumerator.Current => dataRecord;

        public IEnumerator<DataRecord> GetEnumerator()
        {
            return (IEnumerator<DataRecord>)this;
        }


        public bool MoveNext()
        {
            if (this.disposedValue)
            {
                return false;
            }
            if (this.dataRecord == null)
            {
                this.dataRecord = new DataRecord(this);
                if (this.dataRecord.PrepNextRecord())
                {
                    return true;
                }
                else
                {
                    this.Dispose();
                    return false;
                }
            }
            else
            {
                if (this.dataRecord.Last())
                {
                    this.Dispose();
                    return false;
                }
                else
                {
                    if (this.dataRecord.PrepNextRecord())
                    {
                        return true;
                    }
                    else
                    {
                        this.Dispose();
                        return false;
                    }
                }
            }

        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this;
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (this.dBConnection != null)
                    {
                        this.dBConnection = null;
                    }
                    if (this.dbrdr != null)
                    {
                        try { this.dbrdr.Close(); } catch (Exception) { }
                        this.dbrdr = null;
                    }
                    if (this.dbcmd != null)
                    {
                        try { this.dbcmd.Cancel(); } catch (Exception) { }
                        this.dbcmd = null;
                    }
                    if (this.dbcn != null)
                    {
                        try { this.dbcn.Close(); } catch (Exception) { }
                        this.dbcn = null;
                    }
                    if (this.columns != null)
                    {
                        this.columns.Clear();
                        this.columns = null;
                    }
                    if (this.coltypes != null)
                    {
                        this.coltypes = null;
                    }
                    if (this.recData != null)
                    {
                        this.recData = null;
                    }
                }

                disposedValue = true;
            }
        }

        public void WriteCSV(Object output, bool forceClose = false)
        {
            this.WriteDVL(output, "", "", "",forceClose:forceClose);
        }

        public void WriteDVL(Object output, string valdelim, string txtpar, string rowdelim,bool forceClose=false,String datetimeformat=null)
        {
            System.IO.TextWriter sout = null;
            if (output is System.IO.StreamWriter)
            {
                sout = (System.IO.StreamWriter)output;
            } else if (output is System.IO.TextWriter)
            {
                sout = (System.IO.TextWriter)output;
            }

            if (sout != null)
            {
                if (txtpar == "")
                {
                    txtpar = "\"";
                }
                if (valdelim == "")
                {
                    valdelim = ",";
                }
                for (var coli = 0; coli < this.fieldcnt; coli++)
                {
                    sout.Write(txtpar + this.columns[coli].Replace(txtpar, txtpar + txtpar) + txtpar);
                    if (coli < this.fieldcnt - 1)
                    {
                        sout.Write(valdelim);
                    }
                }
                if (rowdelim == "")
                {
                    sout.WriteLine();
                }
                else
                {
                    sout.Write(rowdelim);
                }
                foreach (var rec in this)
                {
                    for (var coli = 0; coli < this.fieldcnt; coli++)
                    {
                        var val = rec.Data[coli];
                        if (val != null)
                        {
                            if (val is DateTime)
                            {
                                
                                var sdt = ((DateTime)val).ToString(((datetimeformat == null || datetimeformat == "") ? "yyyy-MM-dd HH:mm:ss" : datetimeformat));
                                if (sdt.IndexOf(".") > 0)
                                {
                                    var dbl = (Double.Parse(sdt.Substring(sdt.IndexOf(".")+1))/1000).ToString().Replace(",",".");
                                    sdt = sdt.Substring(0, sdt.IndexOf(".")) + dbl;
                                }
                                sout.Write(sdt);
                            }
                            else if (val is int)
                            {
                                sout.Write((int)val);
                            }
                            else if (val is long)
                            {
                                sout.Write((long)val);
                            }
                            else if (val is float)
                            {
                                var s = val.ToString();
                                if (s.IndexOf(".") == -1 && s.IndexOf(",") == -1)
                                {
                                    sout.Write(long.Parse(s));
                                }
                                else
                                {
                                    sout.Write(val);
                                }
                            }
                            else if (val is double)
                            {
                                var s = val.ToString();
                                if (s.IndexOf(".") == -1&& s.IndexOf(",") == -1)
                                {
                                    sout.Write(long.Parse(s));
                                }
                                else
                                {
                                    sout.Write(val);
                                }
                            }
                            else if (val is Decimal)
                            {
                                var s = val.ToString();
                                if (s.IndexOf(".") == -1 && s.IndexOf(",") == -1)
                                {
                                    sout.Write(long.Parse(s));
                                }
                                else
                                {
                                    sout.Write(val);
                                }
                            }
                            else
                            {
                                sout.Write(txtpar + val.ToString().Replace(txtpar, txtpar + txtpar) + txtpar);
                            }
                        }

                        if (coli < this.fieldcnt - 1)
                        {
                            sout.Write(valdelim);
                        }
                    }
                    if (rowdelim == "")
                    {
                        sout.WriteLine();
                    }
                    else
                    {
                        sout.Write(rowdelim);
                    }
                }

                sout.Flush();

                if (forceClose)
                {
                    if (sout is System.IO.TextWriter)
                    {
                        ((System.IO.TextWriter)sout).Close();
                    }
                    else if (sout is System.IO.StreamWriter)
                    {
                        ((System.IO.StreamWriter)output).Close();
                    }
                }
            }
        }

        public void WriteJSON(Object output,bool forceClose=false)
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
                jsonWrtr.WriteStartArray();
                jsonWrtr.WriteStartArray();
                var coli = 0;
                
                foreach (var col in this.columns)
                {
                    jsonWrtr.WriteStartObject();
                        jsonWrtr.WritePropertyName(col);
                        jsonWrtr.WriteValue(this.coltypes[coli++].Name);
                    jsonWrtr.WriteEndObject();
                    //jsonWrtr.WriteValue(col);
                }
                jsonWrtr.WriteEndArray();
                jsonWrtr.WriteStartArray();
                foreach (var rec in this)
                {
                    jsonWrtr.WriteStartArray();
                    foreach (var val in rec.Data)
                    {
                        if (val != null)
                        {
                            if (val is int)
                            {
                                jsonWrtr.WriteValue((int)val);
                            }
                            else if (val is long)
                            {
                                jsonWrtr.WriteValue((long)val);
                            }
                            else if (val is float)
                            {
                                var s = val.ToString();
                                if (s.IndexOf(".") == -1 && s.IndexOf(",") == -1)
                                {
                                    jsonWrtr.WriteValue(long.Parse(s));
                                }
                                else
                                {
                                    jsonWrtr.WriteValue(val);
                                }
                            }
                            else if (val is double)
                            {
                                var s = val.ToString();
                                if (s.IndexOf(".") == -1 && s.IndexOf(",") == -1)
                                {
                                    jsonWrtr.WriteValue(long.Parse(s));
                                }
                                else
                                {
                                    jsonWrtr.WriteValue(val);
                                }
                            }
                            else if (val is Decimal)
                            {  
                                var s = val.ToString();
                                if (s.IndexOf(".") == -1 && s.IndexOf(",") == -1)
                                {
                                    jsonWrtr.WriteValue(long.Parse(s));
                                }
                                else
                                {
                                    jsonWrtr.WriteValue(val);
                                }
                            }
                            else
                            {
                                jsonWrtr.WriteValue(val);
                            }
                        }
                        else
                        {
                            jsonWrtr.WriteValue(val);
                        }
                    }
                    jsonWrtr.WriteEndArray();
                }
                jsonWrtr.WriteEndArray();
                jsonWrtr.WriteEndArray();

                jsonWrtr.Flush();

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

        public void WriteHTML(Object output, bool forceClose = false)
        {
            System.IO.TextWriter sout = null;
            if (output is System.IO.StreamWriter)
            {
                sout = (System.IO.StreamWriter)output;
            } else if (output is System.IO.TextWriter)
            {
                sout = (System.IO.TextWriter)output;
            }

            if (sout != null)
            {
                sout.Write("<table>");
                sout.Write("<thead>");
                sout.Write("<tr>");
                foreach (var col in this.columns)
                {
                    sout.Write("<td>"); sout.Write(col); sout.Write("</td>");
                }
                sout.Write("</tr>");
                sout.Write("</thead>");
                sout.Write("<tbody>");
                foreach (var rec in this)
                {
                    sout.Write("<tr>");
                    foreach (var val in rec.Data)
                    {
                        sout.Write("<td>");
                        if (val is DateTime)
                        {
                            var sdt = ((DateTime)val).ToString("yyyy-MM-ddTHH:mm:ss.fff");
                            if (sdt.IndexOf(".") > 0)
                            {
                                sdt = sdt.Substring(0, sdt.IndexOf(".")) + Double.Parse("0" + sdt.Substring(sdt.IndexOf("."))).ToString().Substring(1);
                            }
                            sout.Write(sdt);
                        }
                        else if (val != null)
                        {
                            if (val is int)
                            {
                                sout.Write((int)val);
                            }
                            else if (val is long)
                            {
                                sout.Write((long)val);
                            }
                            else if (val is float)
                            {
                                var s = val.ToString();
                                if (s.IndexOf(".") == -1)
                                {
                                    sout.Write(long.Parse(s));
                                }
                                else
                                {
                                    sout.Write(val);
                                }
                            }
                            else if (val is double)
                            {
                                var s = val.ToString();
                                if (s.IndexOf(".") == -1)
                                {
                                    sout.Write(long.Parse(s));
                                }
                                else
                                {
                                    sout.Write(val);
                                }
                            }
                            else if (val is Decimal)
                            {
                                var s = val.ToString();
                                if (s.IndexOf(".") == -1)
                                {
                                    sout.Write(long.Parse(s));
                                }
                                else
                                {
                                    sout.Write(val);
                                }
                            }
                            else
                            {
                                sout.Write(val);
                            }
                        }
                        sout.Write("</td>");
                    }
                    sout.Write("</tr>");
                }
                sout.Write("</tbody>");
                sout.Write("<table>");
                sout.Flush();

                if (forceClose)
                {
                    if (sout is System.IO.TextWriter)
                    {
                        ((System.IO.TextWriter)sout).Close();
                    }
                    else if (sout is System.IO.StreamWriter)
                    {
                        ((System.IO.StreamWriter)output).Close();
                    }
                }
                sout = null;
            }
            GC.SuppressFinalize(this);
        }

        public String JSONString() {
            var strbuilder = new StringBuilder();
            var swtr = new StringWriter(strbuilder);
               
            this.WriteJSON(swtr);
            swtr.Flush();

            swtr.Close();
            swtr = null;
            return strbuilder.ToString(0, strbuilder.Length);
        }

        ~DataReader()
        {
            Dispose(!disposedValue);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void Populate(Dictionary<string, object> map, params string[] fieldstomap)
        {
            if (this.columns != null && this.recData != null && this.columns.Count == this.recData.Length)
            {
                if (map != null)
                {
                    var forceAddingVals = map.Count == 0;
                    if (fieldstomap != null && fieldstomap.Length > 0)
                    {
                        foreach (var fldstm in fieldstomap) {
                            var fnm = (fldstm.IndexOf("|") > 0 ? fldstm.Substring(0, fldstm.IndexOf("|")) : fldstm).Trim();
                            var frcnme= (fldstm.IndexOf("|") > 0 ? fldstm.Substring(fldstm.IndexOf("|")+1) : fldstm).Trim();

                            if (this.columns.IndexOf(frcnme) > -1) {
                                if (map.ContainsKey(fnm))
                                {
                                    map[fnm] = this.recData[this.columns.IndexOf(frcnme)];
                                }
                                else if (forceAddingVals) {
                                    map.Add(fnm, this.recData[this.columns.IndexOf(frcnme)]);
                                }
                            }
                        }
                    } else {
                        foreach (var fnm in this.columns)
                        {
                            if (this.columns.IndexOf(fnm) > -1)
                            {
                                if (map.ContainsKey(fnm))
                                {
                                    map[fnm] = this.recData[this.columns.IndexOf(fnm)];
                                }
                                else if (forceAddingVals)
                                {
                                    map.Add(fnm, this.recData[this.columns.IndexOf(fnm)]);
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion
    }
}
