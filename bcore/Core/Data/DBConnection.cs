using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Reflection;
using Bcoring.ES6.Expressions;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Lnksnk.Core.Data
{
    public class DBConnection
    {
        private DbProviderFactory providerFactory = null;
        private string cnstring;
        private string qryprmsql = "";
        private DBConnection dBConnectionref = null;

        internal bool remoteConnection = false;
        internal string hostPath = "";

        public DBConnection(string factoryassembly, string cnstring, string qryprmsql = "") : this(invokeDbProviderFactory(factoryassembly,ref qryprmsql), cnstring.Replace('\0', '@'))
        {
            this.qryprmsql = qryprmsql;
            this.dBConnectionref = this;
            this.remoteConnection = factoryassembly != null && factoryassembly.ToLower().Equals("remote");
            this.hostPath = cnstring.Replace('\0', '@');
        }

        private static DbProviderFactory invokeDbProviderFactory(string factoryassembly,ref string qryprmsql)
        {
            try
            {
                if (!factoryassembly.ToLower().Equals("remote"))
                {
                    DbProviderFactory dbdrvr = null;
                    var asmpath = factoryassembly + "";
                    if (factoryassembly.Equals("Microsoft.Data.SqlClient.SqlClientFactory"))
                    {
                        dbdrvr = Microsoft.Data.SqlClient.SqlClientFactory.Instance;
                    }
                    else
                    {
                        dbdrvr = InvokeDbProviderFactoryFromAsm(asmpath, factoryassembly);
                    }
                    return dbdrvr;
                }
                return null;
            }
            catch (Exception ex)
            {
                Logging.Log.LOG().Error(ex);
                return null;
            }
        }

        private static Dictionary<string, Type> dbproviderfactorytypes = new Dictionary<string, Type>();

        private static DbProviderFactory InvokeDbProviderFactoryFromAsm(string asmpath,string dbproviderfactorytypename,string instancefieldname="Instance") {
            if (dbproviderfactorytypes.ContainsKey(dbproviderfactorytypename)) {
                var drvinstance = dbproviderfactorytypes[dbproviderfactorytypename].GetField(instancefieldname);
                if (drvinstance != null)
                {
                    return (DbProviderFactory)drvinstance.GetValue(null);
                }
            }
            var root = Type.GetType("Lnksnk.Core.Data.DBConnection").Assembly.GetName().CodeBase;
            root = (root = root.Replace("\\", "/"));
            root = root.Substring(0, root.LastIndexOf("/"));
            if (root.StartsWith("file:///"))
            {
                root = root.Substring("file:///".Length);
            }
            if (!root.EndsWith("/")) root = root + "/";
            Type drvtype = null;
            var drvasm = Assembly.GetExecutingAssembly();
            drvtype = drvasm.GetType(dbproviderfactorytypename);
            if (drvtype == null)
            {
                drvasm = null;
                while (drvasm==null && asmpath.Length > 0) {
                    if (asmpath.LastIndexOf(".") > 0) {
                        if (File.Exists(root + asmpath + ".dll")) {
                            drvasm=Assembly.LoadFrom(root + asmpath + ".dll");
                            break;
                        }
                        asmpath = asmpath.Substring(0, asmpath.LastIndexOf("."));
                    } else {
                        if (File.Exists(root + asmpath + ".dll"))
                        {
                            drvasm = Assembly.LoadFrom(root + asmpath + ".dll");
                            break;
                        }
                    }
                }
                if (drvasm != null) {
                    if ((drvtype = drvasm.GetType(dbproviderfactorytypename)) != null)
                    {
                        if (typeof(DbProviderFactory).IsAssignableFrom(drvtype))
                        {
                            var drvinstance = drvtype.GetField(instancefieldname);
                            if (drvinstance != null)
                            {
                                if (!dbproviderfactorytypes.ContainsKey(dbproviderfactorytypename)) {
                                    dbproviderfactorytypes.Add(dbproviderfactorytypename, drvtype);
                                }
                                return (DbProviderFactory)drvinstance.GetValue(null);
                            }
                        }
                    }
                }
            }
            else if (typeof(DbProviderFactory).IsAssignableFrom(drvtype))
            {
                var drvinstance = drvtype.GetField(instancefieldname);
                if (drvinstance != null)
                {
                    if (!dbproviderfactorytypes.ContainsKey(dbproviderfactorytypename))
                    {
                        dbproviderfactorytypes.Add(dbproviderfactorytypename, drvtype);
                    }
                    return (DbProviderFactory)drvinstance.GetValue(null);
                }
            }
            return null;
        }

        internal string Parenthises()
        {
            return "@@";
        }

        public DBConnection(DbProviderFactory providerFactory, string cnstring)
        {
            this.providerFactory = providerFactory;
            this.cnstring = cnstring;
        }

        internal static Exception DataExecute(out Exception lastExecErr,DBConnection dbconnection,string query, DataExecutor dataExecutor, out DataReader dataReader, params object[] parameters)
        {
            lastExecErr = null;
            object[] outprms = null;
            IO.ActiveReader.PrepActiveArgs(out outprms, args: parameters);
            parameters = outprms;
            dataReader = null;
            string sqlparenthesis = "@@";
            if (parameters != null && parameters.Length >= 1 && parameters[0] is string)
            {
                if (!(parameters[0] = ((string)parameters[0]).Trim()).Equals(""))
                {
                    sqlparenthesis = (string)parameters[0];
                }
                parameters = parameters[1..];
            }

            var prms = dataExecutor==null?new Dictionary<string, object>(): dataExecutor.prms ?? (dataExecutor.prms = new Dictionary<string, object>());
            
            var remotedbalias = "";
            if (dbconnection != null && dbconnection.remoteConnection)
            {
                if (dbconnection.hostPath.Contains("/dbms-"))
                {
                    remotedbalias = dbconnection.hostPath.Substring(dbconnection.hostPath.IndexOf("/dbms-") + "/dbms-".Length);
                    if (remotedbalias.IndexOf("/") > 0)
                    {
                        remotedbalias = remotedbalias.Substring(0, remotedbalias.IndexOf("/"));
                        if (dataExecutor!=null)
                        {
                            if (prms.ContainsKey(remotedbalias + "-execute"))
                            {
                                prms[remotedbalias + "-execute"] = query;
                            } else
                            {
                                prms.Add(remotedbalias + "-execute", query);
                            }
                            
                        } else
                        {
                            if (prms.ContainsKey(remotedbalias + "-query"))
                            {
                                prms[remotedbalias + "-query"] = query;
                            }
                            else
                            {
                                prms.Add(remotedbalias + "-query", query);
                            }
                        }
                    }
                    else
                    {
                        remotedbalias = "";
                    }
                }
            }
            var prmslist= dataExecutor == null ? new List<string>() : dataExecutor.prmslist ?? (dataExecutor.prmslist = new List<string>());

            if (parameters != null && parameters.Length > 0)
            {
                var pnamesFound = new List<string>();
                foreach (var prm in parameters)
                {
                    if (prm is Dictionary<string, object>)
                    {
                        if (((Dictionary<string, object>)prm).Count > 0)
                        {
                            foreach (var prmkv in (Dictionary<string, object>)prm)
                            {
                                if (remotedbalias == "")
                                {
                                    pnamesFound.Add(prmkv.Key);
                                }
                                if (prms.ContainsKey(remotedbalias == null || remotedbalias == "" ? prmkv.Key : (remotedbalias + ":" + prmkv.Key)))
                                {
                                    prms[remotedbalias == null || remotedbalias == "" ? prmkv.Key : (remotedbalias + ":" + prmkv.Key)] = prmkv.Value;
                                }
                                else
                                {
                                    prms.Add(remotedbalias == null || remotedbalias == "" ? prmkv.Key : (remotedbalias + ":" + prmkv.Key), prmkv.Value);
                                }
                            }
                        }
                    }
                    else if (prm is Data.DataRecord)
                    {
                        foreach (var prmkv in ((Data.DataRecord)prm).Colums)
                        {
                            if (remotedbalias == "")
                            {
                                pnamesFound.Add(prmkv);
                            }
                            if (prms.ContainsKey(remotedbalias == null || remotedbalias == "" ? prmkv : (remotedbalias + ":" + prmkv)))
                            {
                                prms[remotedbalias == null || remotedbalias == "" ? prmkv : (remotedbalias + ":" + prmkv)] = ((Data.DataRecord)prm)[prmkv];
                            }
                            else
                            {
                                prms.Add(remotedbalias == null || remotedbalias == "" ? prmkv : (remotedbalias + ":" + prmkv), ((Data.DataRecord)prm)[prmkv]);
                            }
                        }
                    }
                    else if (prm is Data.DataReader)
                    {
                        if (((Data.DataReader)prm).Current != null)
                        {
                            foreach (var prmkv in ((Data.DataReader)prm).Columns)
                            {
                                if (remotedbalias == "")
                                {
                                    pnamesFound.Add(prmkv);
                                }
                                if (prms.ContainsKey(remotedbalias == null || remotedbalias == "" ? prmkv : (remotedbalias + ":" + prmkv)))
                                {
                                    prms[remotedbalias == null || remotedbalias == "" ? prmkv : (remotedbalias + ":" + prmkv)] = ((Data.DataReader)prm).Current[prmkv];
                                }
                                else
                                {
                                    prms.Add(remotedbalias == null || remotedbalias == "" ? prmkv : (remotedbalias + ":" + prmkv), ((Data.DataReader)prm).Current[prmkv]);
                                }
                            }
                        }
                    }
                }
                if (pnamesFound.Count == 0)
                {
                    if (remotedbalias == "" && prms.Count > 0)
                    {
                        var pkvs = prms.Keys.ToArray();
                        foreach (var pkv in pkvs)
                        {
                            prms[pkv] = null;
                            prms.Remove(pkv);
                        }
                        prms.Clear();
                    }
                }
                else
                {
                    if (remotedbalias == "")
                    {
                        var pnamestoremove = new List<string>();
                        var pkvs = prms.Keys.ToArray();
                        foreach (var pkv in pkvs)
                        {
                            if (!pnamesFound.Contains(pkv)) pnamestoremove.Add(pkv);
                        }
                        foreach (var pkv in pnamestoremove)
                        {
                            prms[pkv] = null;
                            prms.Remove(pkv);
                        }
                        pnamestoremove.Clear();
                    }
                    pnamesFound.Clear();
                }
            }
            try
            {
                if (dbconnection != null && dbconnection.remoteConnection)
                {
                    bool json = dbconnection.hostPath.EndsWith(".json");
                    bool csv = dbconnection.hostPath.EndsWith(".csv");
                    string csvcoldelim = prms.ContainsKey("coldelim") && prms["coldelim"] is string ? (string)prms["coldelim"] : "";
                    string csvrowdelim = prms.ContainsKey("rowdelim") && prms["rowdelim"] is string ? (string)prms["rowdelim"] : "\r\n";
                    bool csvcolheaders = prms.ContainsKey("colheaders") ? prms["colheaders"] is bool ? (bool)prms["rowdelim"] ? prms["colheaders"] is string && (((string)prms["colheaders"]).ToLower() == "y" || ((string)prms["colheaders"]).ToLower() == "true") ? true : true : true : true : true;
                    string[] csvcoltypes = prms.ContainsKey("coltypes") ? prms["colheaders"] is string ? ((string)prms["colheaders"]).Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries) : prms["colheaders"] is string[]? (string[])prms["colheaders"] : null : null;
                    string[] csvcolumns = prms.ContainsKey("coltypes") ? prms["columns"] is string ? ((string)prms["columns"]).Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries) : prms["columns"] is string[]? (string[])prms["columns"] : null : null; ;

                    HttpClient httpClient = new HttpClient(new HttpClientHandler()
                    {
                        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    });
                    MultipartFormDataContent mpartcontentStream = null;

                    HttpRequestMessage httpRequestMessage = new HttpRequestMessage(parameters == null || parameters.Length == 0 ? (prms != null && prms.Count > 0) ? HttpMethod.Post : HttpMethod.Get : HttpMethod.Post, dbconnection.hostPath)
                    {
                        Content = parameters == null || parameters.Length == 0 ? (prms != null && prms.Count > 0) ? (mpartcontentStream = new MultipartFormDataContent("----DataConnection")) : null : (mpartcontentStream = new MultipartFormDataContent("----DataConnection"))
                    };

                    if (prms != null && prms.Count > 0)
                    {
                        foreach (var prmkey in prms.Keys)
                        {
                            object val = prms[prmkey];
                            if (val == null)
                            {
                                mpartcontentStream.Add(new StringContent(""), prmkey);
                            }
                            else
                            {
                                if (val is string)
                                {
                                    mpartcontentStream.Add(new StringContent((string)val), prmkey);
                                }
                                else if (val is int || val is long || val is float || val is double || val is decimal)
                                {
                                    mpartcontentStream.Add(new StringContent(val.ToString()), prmkey);
                                }
                            }
                        }
                    }

                    HttpResponseMessage httpResponseMessage = null;
                    Task.WaitAll(Task.Run(async () =>
                    {
                        httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
                    }));
                    if (httpResponseMessage != null)
                    {
                        Stream strm = null;
                        Task.WaitAll(Task.Run(async () =>
                        {
                            strm = await httpResponseMessage.Content.ReadAsStreamAsync();
                        }));
                        if (strm != null)
                        {
                            if (dataExecutor == null)
                            {
                                dataReader=new DataReader(strm, json, csv, csvcoldelim, csvrowdelim, csvcolheaders, csvcoltypes, csvcolumns);
                            }
                            else
                            {
                                if (json)
                                {

                                }
                                else
                                {
                                    return null;
                                }
                            }
                        }
                    }
                }
                else if (dataExecutor != null)
                {
                    if (dataExecutor.dbcmd == null)
                    {
                        dataExecutor.dbcmd = dataExecutor.dbcn.CreateCommand();
                    }
                    DBConnection.populateCmdParams(sqlparenthesis, dbconnection.remoteConnection, ref dbconnection, ref dataExecutor.dbcmd, ref query, true, ref prmslist, ref prms);
                    dataExecutor.dbcmd.ExecuteNonQuery();
                }
                else {
                    DbConnection dbcn = null;
                    DbCommand dbcmd = null;
                    DbDataReader dbrdr = null;
                    try
                    {
                        dbcn = dbconnection.providerFactory.CreateConnection();
                        dbcn.ConnectionString = dbconnection.cnstring;
                        dbcn.Open();
                        dbcmd = dbcn.CreateCommand();
                        populateCmdParams(sqlparenthesis, dbconnection.remoteConnection, ref dbconnection, ref dbcmd, ref query, true, ref prmslist, ref prms);
                        if (dbcmd.CommandText.Equals(""))
                        {
                            dbcmd.CommandText = query;
                        }
                        dbrdr = dbcmd.ExecuteReader();
                        dataReader = new DataReader(dbconnection, dbcn, dbcmd, dbrdr);
                    }
                    catch (Exception e)
                    {
                        lastExecErr = e;
                        if (dbrdr != null)
                        {
                            try { dbrdr.Close(); } catch (Exception) { }
                            dbrdr = null;
                        }
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
                        Logging.Log.LOG().Error(e);
                        return e;
                    }
                }
                return null;
            }
            catch (Exception e)
            {
                lastExecErr = e;
                Logging.Log.LOG().Error(e);
                return e;
            }
        }

        public DataReader Query(string query, params object[] parameters)
        {
            Exception lastExecErr = null;
            DataReader dataReader = null;
            if (DataExecute(out lastExecErr, this, query, null,out dataReader, parameters: parameters) == null) {
                return dataReader == null ? DataReader.EmptyDataReader() : dataReader;
            }
            return DataReader.EmptyDataReader();
        }

        public DataExecutor Executor()
        {
            DataExecutor dataExecutor = null;
            
            DbConnection dbcn = null;
            try
            {
                if (this.remoteConnection)
                {
                    dataExecutor = new DataExecutor(this, dbcn);
                }
                else
                {
                    dbcn = this.providerFactory.CreateConnection();
                    dbcn.ConnectionString = this.cnstring;
                    dbcn.Open();
                    dataExecutor = new DataExecutor(this, dbcn);
                }
                return dataExecutor;
            }
            catch (Exception e)
            {
                Logging.Log.LOG().Error(e);
                if (dbcn != null)
                {
                    try { dbcn.Close(); } catch (Exception) { }
                    dbcn = null;
                }
                return null;
            }
        }

        internal static void populateCmdParams(string prmparenthesis,bool remoteRquest, ref DBConnection dBConnection, ref DbCommand dbcmd, ref string query, bool parseQuery, ref List<string> paramslist, ref Dictionary<string, object> prms)
        {
            var qrytoexecute = "" + query;
            if (parseQuery)
            {
                dbcmd.Parameters.Clear();
                paramslist.Clear();
                var prsqry = "" + (query);
                qrytoexecute = "";
                char prvc = (char)0;
                bool istxt = false;
                char[] prmlbl = ((prmparenthesis == null||(prmparenthesis=prmparenthesis.Trim()).Equals(""))?"@@": prmparenthesis).ToCharArray();
                int prmlbli = 0;
                bool isprm = false;
                var prmname = "";

                foreach (var prsq in prsqry.AsSpan())
                {
                    if (!isprm&&istxt)
                    {
                        if (prsq == '\'')
                        {
                            if (prvc != prsq)
                            {
                                istxt = false;
                            }
                        }
                        qrytoexecute += prsq;
                    }
                    else
                    {
                        if (isprm)
                        {
                            if (prmlbli > 0 && prmlbl[prmlbli - 1] == prvc && prmlbl[prmlbli] != prsq)
                            {
                                if (!prmname.Equals(""))
                                {
                                    prsqry += ("@@" + prmname);
                                    prmname = "";
                                }
                                foreach (var pc in prmlbl.AsSpan(0, prmlbli))
                                {
                                    qrytoexecute += pc;
                                }
                                prmlbli = 0;
                                prvc = prsq;
                                qrytoexecute += prsq;
                                if (prsq == '\'')
                                {
                                    istxt = true;
                                }
                                isprm = false;

                                continue;
                            }
                            if (prmlbl[prmlbli] == prsq)
                            {
                                prmlbli++;
                                if (prmlbl.Length == prmlbli)
                                {
                                    if (prms.ContainsKey(prmname))
                                    {
                                        if (prms[prmname] == null)
                                        {
                                            qrytoexecute += "NULL";
                                        }
                                        else
                                        {
                                            paramslist.Add(prmname);
                                            if (dBConnection.qryprmsql.Equals("?") || dBConnection.qryprmsql.Equals(""))
                                            {
                                                qrytoexecute += "?";
                                            }
                                            else
                                            {
                                                qrytoexecute += (dBConnection.qryprmsql + (paramslist.Count - 1).ToString());
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (!prmname.Equals(""))
                                        {
                                            qrytoexecute += ("@@" + prmname + "@@");
                                            prmname = "";
                                        }
                                    }
                                    prmlbli = 0;
                                    isprm = false;
                                    continue;
                                }
                            }
                            else
                            {
                                prmname += prsq;
                            }
                        }
                        else
                        {
                            if (prmlbli > 0 && prmlbl[prmlbli - 1] == prvc && prmlbl[prmlbli] != prsq)
                            {
                                foreach (var pc in prmlbl.AsSpan(0, prmlbli))
                                {
                                    qrytoexecute += pc;
                                }
                                prmlbli = 0;
                            }
                            if (prsq == '\'')
                            {
                                qrytoexecute += prsq;
                                istxt = true;
                                prvc = (char)0;
                                continue;
                            }
                            if (prmlbl[prmlbli] == prsq)
                            {
                                prmlbli++;
                                if (prmlbl.Length == prmlbli)
                                {
                                    prmlbli = 0;
                                    isprm = true;
                                    prmname = "";
                                    continue;
                                }
                            }
                            else
                            {
                                if (prmlbli > 0)
                                {
                                    foreach (var pc in prmlbl.AsSpan(0, prmlbli))
                                    {
                                        qrytoexecute += pc;
                                    }
                                    prmlbli = 0;
                                }
                                qrytoexecute += prsq;
                            }
                        }
                    }
                    prvc = prsq;
                }
                if (isprm) {
                    if (!prmname.Equals("")) {
                        qrytoexecute += prmname;
                    }
                    if (prmlbli > 0)
                    {
                        foreach (var pc in prmlbl.AsSpan(0, prmlbli))
                        {
                            qrytoexecute += pc;
                        }
                    }
                }
            }
            if (paramslist.Count > 0)
            {
                dbcmd.Parameters.Clear();
                var prmcnt = 0;
                foreach (var prmnme in paramslist)
                {
                    if (prms.ContainsKey(prmnme)) {
                        var dbcmdprm=dbcmd.CreateParameter();
                        dbcmdprm.ParameterName = dBConnection.qryprmsql + prmcnt.ToString();
                        dbcmdprm.Value = prms[prmnme];
                        dbcmd.Parameters.Add(dbcmdprm);
                    }
                    prmcnt++;
                }
            }
            dbcmd.CommandText = qrytoexecute;
        }
    }
}
