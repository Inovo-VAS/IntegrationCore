using Lnksnk.Core.Data;
using Lnksnk.Core.IO;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO.Compression;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Lnksnk.Core.Servicing;

namespace Lnksnk.Core.Net.Web
{
    public class BaseHandler : IEnumerable<ResourceBase>, IEnumerator<ResourceBase>,Lnksnk.Core.IO.ActiveSourceFinder
    {

        private string lastPathRoot = "/";

        private string root = "";

        private Parameters parameters = null;

        public Parameters Parameters => this.parameters;

        protected string userHostAddress = "";
        public string UserHostAddress => this.userHostAddress;

        private Web web = null;
        public Web Web => this.web;

        public BaseHandler(Web web) {
            this.web = web;
            this.parameters = new Parameters(this);
            var root = Environment.Environment.ENV().Root.Replace("\\", "/");
            if (root.Equals(""))
            {
                root = Endpoints.ContextPath();
            }
            if (root.EndsWith("/") && !root.Equals("/"))
            {
                root = root.Substring(0, root.Length - 1);
            }
            this.root = root;
            var dbaliasses = this.StandardParameter("dbalias");
            if (dbaliasses.Length > 0)
            {
                this.DBCNs(dbaliasses);
            }
            this.activeMap.Add("endpointcommands", Endpoints.commands);
            if (web != null)
            {
                this.activeMap.Add("web", web);
            }
        }

        public void AddPath(params object[] pathargs)
        {
            if (pathargs != null && pathargs.Length > 0)
            {
                var pathargsi = 0;
                foreach (var patharg in pathargs)
                {
                    var nextpatharg = pathargsi < (pathargs.Length - 1) ? pathargs[pathargsi + 1] != null && pathargs[pathargsi + 1] is Dictionary<string, object> ? (Dictionary<string, object>)pathargs[pathargsi + 1] : null : null;
                    pathargsi++;
                    if (patharg is string)
                    {
                        var path = (string)patharg;
                        var pathstngs = nextpatharg;
                        if (path != null && !path.Equals(""))
                        {
                            string lastpathroot = this.lastPathRoot;
                            if ((path.IndexOf("|") > 0))
                            {
                                while (path.IndexOf("|") > 0)
                                {
                                    var pth = path.Substring(0, path.IndexOf("|")).Trim();
                                    path = path.Substring(path.IndexOf("|") + 1);
                                    if (!pth.Equals(""))
                                    {
                                        if (pth.StartsWith("/"))
                                        {
                                            lastpathroot = pth.Substring(0, pth.LastIndexOf("/") + 1);
                                        }
                                        else
                                        {
                                            pth = lastpathroot + pth;
                                        }
                                        this.paths.Add(pth);
                                        this.pathsettings.Add(pathstngs);
                                    }
                                }
                                if (path.IndexOf("|") == -1 && !path.Equals(""))
                                {
                                    if (path.StartsWith("/"))
                                    {
                                        lastpathroot = path.Substring(0, path.LastIndexOf("/") + 1);
                                    }
                                    else
                                    {
                                        path = lastpathroot + path;
                                    }
                                    this.paths.Add(path);
                                    this.pathsettings.Add(pathstngs);
                                }
                            }
                            else
                            {
                                if (path.StartsWith("/"))
                                {
                                    lastpathroot = path.Substring(0, path.LastIndexOf("/") + 1);
                                }
                                else
                                {
                                    path = lastpathroot + path;
                                }
                                this.paths.Add(path);
                                this.pathsettings.Add(pathstngs);
                            }
                        }
                    }
                }
            }
        }

        internal Dictionary<String, Object> activeMap = new Dictionary<string, object>();

        private Dictionary<string, DBConnection> dbcntns = null;

        private Dictionary<string, Assembly> asmMap = new Dictionary<string, Assembly>();
        internal Assembly LoadAsm(string asmpath)
        {
            Assembly asm = null;
            var cntxroot = Endpoints.ContextPath();
            while (asm == null && asmpath.Length > 0)
            {
                if (this.asmMap.ContainsKey(asmpath))
                {
                    asm = this.asmMap[asmpath];
                    break;
                }
                if (asmpath.LastIndexOf(".") > 0)
                {
                    if (File.Exists(root + asmpath + ".dll"))
                    {
                        if ((asm = Assembly.LoadFrom(root + asmpath + ".dll")) != null)
                        {
                            this.asmMap.Add(asmpath, asm);
                        }
                        break;
                    } else if (File.Exists(cntxroot + asmpath + ".dll"))
                    {
                        if ((asm = Assembly.LoadFrom(cntxroot + asmpath + ".dll")) != null)
                        {
                            this.asmMap.Add(asmpath, asm);
                        }
                        break;
                    }
                    asmpath = asmpath.Substring(0, asmpath.LastIndexOf("."));
                }
                else
                {
                    if (File.Exists(root + asmpath + ".dll"))
                    {
                        if ((asm = Assembly.LoadFrom(root + asmpath + ".dll")) != null)
                        {
                            this.asmMap.Add(asmpath, asm);
                        }
                    }
                    else if (File.Exists(cntxroot + asmpath + ".dll"))
                    {
                        if ((asm = Assembly.LoadFrom(cntxroot + asmpath + ".dll")) != null)
                        {
                            this.asmMap.Add(asmpath, asm);
                        }
                        break;
                    }
                    break;
                }
            }
            return asm;
        }
        internal Type LoadAsmType(Assembly asm, string asmtypename, Type baseType = null)
        {
            Type asmtype = asm.GetType(asmtypename, false, true);
            if (asmtype != null)
            {
                if (baseType != null)
                {
                    if (baseType.IsAssignableFrom(asmtype))
                    {
                        return asmtype;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            return asmtype;
        }

        public virtual Data.DataReader DataReader(params object[] parameters) {
            return this.DataReader(null, parameters: parameters);
        }

        internal Data.DataReader DataReader(HttpRequest asphttprequest, params object[] parameters)
        {
            if (asphttprequest != null)
            {
                var args = (object[])null;
                ActiveReader.PrepActiveArgs(out args, parameters);
                parameters = args;
                if (parameters.Length >= 2 && parameters[0] is string && parameters[1] is string)
                {
                    var dbalias = (string)parameters[0];
                    var sqlquery = (string)parameters[1];
                    parameters = parameters[2..];
                    var dbcn = this.DBCN(dbalias);
                    if (dbcn == null)
                    {
                        return Data.DataReader.EmptyDataReader();
                    }
                    else
                    {
                        if (parameters != null && parameters.Length >= 1)
                        {
                            if (parameters[0] == null)
                            {
                                parameters = parameters[1..];
                            }
                            else
                            {
                                for (var pi = 0; pi < parameters.Length; pi++)
                                {
                                    if (parameters[pi] != null && this.parameters.Standard != null && (parameters[pi]==this.parameters || parameters[pi] == this.parameters.Standard))
                                    {
                                        var prmsset = new Dictionary<string, object>();
                                        foreach (var stdnme in this.parameters.Standard.Names)
                                        {
                                            if (!prmsset.ContainsKey(stdnme))
                                            {
                                                prmsset.Add(stdnme, this.parameters.Standard.String(stdnme));
                                            }
                                        }
                                        parameters[pi] = prmsset;
                                        prmsset = null;
                                    }
                                }
                            }
                        }
                        return dbcn.Query(sqlquery, parameters: parameters);
                    }
                }
                else if (parameters == null || parameters.Length == 0)
                {
                    if (asphttprequest.ContentType.Equals("application/json"))
                    {
                        return new Data.DataReader(new System.IO.StreamReader(asphttprequest.BodyReader.AsStream()), json: true);
                    }
                }
            }

            return Data.DataReader.EmptyDataReader();
        }

        public Data.DataExecutor DataExecutor(params object[] parameters)
        {
            var args = (object[])null;
            ActiveReader.PrepActiveArgs(out args, parameters);
            parameters = args;
            if (parameters.Length >= 2 && parameters[0] is string && parameters[1] is string)
            {
                var dbalias = parameters[0] + "";
                var sqlquery = parameters[1] + "";
                parameters = parameters[2..];
                var dbcn = this.DBCN(dbalias);
                if (dbcn == null)
                {
                    return Data.DataExecutor.EmptyDataExecutor();
                }
                else
                {
                    if (parameters != null && parameters.Length >= 1)
                    {
                        if (parameters[0] == null)
                        {
                            parameters = parameters[1..];
                        }
                        else
                        {
                            for (var pi = 0; pi < parameters.Length; pi++)
                            {
                                if (parameters[pi] != null && this.parameters.Standard != null && (parameters[pi] == this.parameters || parameters[pi] == this.parameters.Standard))
                                {
                                    var prmsset = new Dictionary<string, object>();
                                    foreach (var stdnme in this.parameters.Standard.Names)
                                    {
                                        if (!prmsset.ContainsKey(stdnme))
                                        {
                                            prmsset.Add(stdnme, this.parameters.Standard.String(stdnme));
                                        }
                                    }
                                    parameters[pi] = prmsset;
                                }
                            }
                        }
                    }
                    var dbxctr = dbcn.Executor();
                    dbxctr.Execute(sqlquery, parameters);
                    return dbxctr;
                }
            }
            return Data.DataExecutor.EmptyDataExecutor();
        }

        protected List<String> paths = new List<string>();
        protected List<Dictionary<string, object>> pathsettings = new List<Dictionary<string, object>>();

        public String RootPath()
        {
            return this.root;
        }

        private DataPath pathReader = null;
        public ResourceBase Current => pathReader;

        object IEnumerator.Current => pathReader;

        public bool MoveNext()
        {
            if (this.paths.Count > 0)
            {
                var nextpath = this.paths[0];
                this.paths.RemoveAt(0);
                var nextpathsettings = this.pathsettings[0];
                this.pathsettings.RemoveAt(0);
                if (nextpath.LastIndexOf("/") > -1)
                {
                    if (nextpath.StartsWith("/"))
                    {
                        this.lastPathRoot = nextpath.Substring(0, nextpath.LastIndexOf("/") + 1);
                    }
                    else
                    {
                        this.lastPathRoot += nextpath.Substring(0, nextpath.LastIndexOf("/") + 1);
                    }
                }

                if (this.pathReader == null)
                {
                    this.pathReader = new DataPath(this);
                    if (!this.activeMap.ContainsKey("NextAction"))
                    {
                        var webaction = (Interrupting)this.InterruptAction;
                        this.activeMap.Add("NextAction", webaction);
                    }
                    if (this.activeMap.ContainsKey("PathResource"))
                    {
                        this.activeMap["PathResource"] = this.pathReader;
                    }
                    else
                    {
                        this.activeMap.Add("PathResource", this.pathReader);
                    }
                    if (this.pathReader.PrepNextPath(nextpath, Mimetypes.FindExtMimetype(nextpath), true, this.paths.Count == 0, Mimetypes.TextExtension(nextpath), nextpathsettings))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (this.pathReader.PrepNextPath(nextpath, Mimetypes.FindExtMimetype(nextpath), true, this.paths.Count == 0, Mimetypes.TextExtension(nextpath), nextpathsettings))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

            }
            else
            {
                if (this.pathReader != null)
                {
                    this.pathReader = null;
                }
                return false;
            }
        }

        internal long writtenContentLength = 0;


        private object wrtncntlck = new object();
        internal void IncWrittenContent(int incby)
        {
            lock (this.wrtncntlck)
            {
                this.writtenContentLength += incby;
            }
        }

        public virtual bool PathRequestingResponding(ResourceBase pathrsrc, HttpWebRequest request, HttpWebResponse response, Action doneReading = null, Action doneReadingNothing = null)
        {
            if (pathrsrc.DoneReading == null)
            {
                pathrsrc.DoneReading = doneReading;
            }
            if (pathrsrc.DoneReadingNothing == null)
            {
                pathrsrc.DoneReadingNothing = doneReadingNothing;
            }
            if (pathrsrc.InterruptAction == null)
            {
                pathrsrc.InterruptAction = this.InterruptAction;
            }
            //Task.WaitForAll(pathrsrc.WriteAsync(response.BodyWriter.WriteAsync, true));
            //response.BodyWriter.FlushAsync().AsTask().Wait();
            return true;
        }

        public virtual bool PathRequestingResponding(ResourceBase pathrsrc, HttpRequest request, HttpResponse response,Action doneReading=null,Action doneReadingNothing=null) {
            if (pathrsrc.DoneReading == null)
            {
                pathrsrc.DoneReading = doneReading;
            }
            if (pathrsrc.DoneReadingNothing == null)
            {
                pathrsrc.DoneReadingNothing = doneReadingNothing;
            }
            if (pathrsrc.InterruptAction == null)
            {
                pathrsrc.InterruptAction = this.InterruptAction;
            }
            try
            {
                Task.WaitAll(pathrsrc.WriteAsync(response.BodyWriter.WriteAsync, true));
                Task.WaitAll(response.BodyWriter.FlushAsync().AsTask());
            }
            catch (Exception exc) {
                exc = null;
            }
            
            return true;
        }

        public virtual void ExecuteHandler(HttpWebRequest request, HttpWebResponse response, Action doneReading = null, Action doneReadingNothing = null)
        {
            if (doneReading == null)
            {
                doneReading = () => { };
            }
            if (doneReadingNothing == null)
            {
                doneReadingNothing = () => { };
            }
            foreach (var pathrdr in this)
            {
                if (this.PathRequestingResponding(pathrdr, request, response, doneReading, doneReadingNothing))
                {
                    continue;
                }
                else { break; }
            }
            if (this.writtenContentLength == 0)
            {
                /*if (request.Path.Value.Equals("/dbms") && this.dbcntns != null && this.dbcntns.Count > 0)
                {

                }*/
            }
        }

        internal long startOffset = -1;
        internal long lastStartOffset = -1;
        public virtual void ExecuteHandler(HttpRequest request, HttpResponse response, Action doneReading = null, Action doneReadingNothing = null)
        {
            if (doneReading == null) {
                doneReading = ()=>{ };
            }
            if (doneReadingNothing == null) {
                doneReadingNothing = () => { };
            }
            if (request != null) { 
                if (request.Headers.ContainsKey("Range"))
                {
                    string range = request.Headers["Range"];
                    if (range != null && range != "")
                    {
                        if (range.StartsWith("bytes=")) {
                            range = range.Substring("bytes=".Length);
                            if (range.IndexOf("-")>0)
                            {
                                this.startOffset = (this.lastStartOffset = long.Parse(range.Substring(0, range.IndexOf("-"))));
                            }
                        }
                    }
                }
            }
            foreach (var pathrdr in this)
            {
                if (this.PathRequestingResponding(pathrdr, request, response, doneReading, doneReadingNothing))
                {
                    continue;
                }
                else { break; }
            }
            if (this.writtenContentLength == 0)
            {
                if (request.Path.Value.Equals("/dbms") && this.dbcntns != null && this.dbcntns.Count > 0)
                {

                }
            }
        }

        internal void DecWrittenContent(int decby)
        {
            lock (this.wrtncntlck)
            {
                this.writtenContentLength -= decby;
            }
        }

        public void InterruptAction(params object[] args)
        {
            foreach (var arg in args)
            {
                if (arg is string)
                {
                    this.AddPath((string)arg);
                }
                else if (arg is Dictionary<string, object>)
                {
                    var argdictionary = (Dictionary<string, object>)arg;
                    if (argdictionary.Count > 0)
                    {
                        foreach (var kv in argdictionary)
                        {
                            if (kv.Key.Equals("next-path"))
                            {
                                if (kv.Value is string)
                                {
                                    this.AddPath((string)kv.Value);
                                }
                                else if (kv.Value is Dictionary<string, object>)
                                {
                                    var pathinfo = (Dictionary<string, object>)kv.Value;
                                    if (pathinfo.Count > 0)
                                    {
                                        if (pathinfo.ContainsKey("path"))
                                        {
                                            this.AddPath(pathinfo["path"], pathinfo.Remove("path") ? pathinfo : pathinfo);
                                        }
                                    }
                                }
                                else if (kv.Value is List<object> || kv.Value is object[])
                                {
                                    var voarr = (kv.Value is List<object>) ? ((List<object>)kv.Value).ToArray() : (object[])kv.Value;
                                    if (voarr.Length > 0)
                                    {
                                        var voargs = new List<object>();
                                        var prvisstring = false;
                                        foreach (var vrg in voarr)
                                        {
                                            if (vrg is string)
                                            {
                                                prvisstring = true;
                                                voargs.Add(vrg);
                                            }
                                            else if (vrg is Dictionary<string, object>)
                                            {
                                                if (((Dictionary<string, object>)vrg).ContainsKey("path"))
                                                {
                                                    voargs.Add(((Dictionary<string, object>)vrg)["path"]);
                                                    ((Dictionary<string, object>)vrg).Remove("path");
                                                    prvisstring = true;
                                                }
                                                if (prvisstring)
                                                {
                                                    voargs.Add((Dictionary<string, object>)vrg);
                                                }
                                                prvisstring = false;
                                            }
                                            else
                                            {
                                                prvisstring = false;
                                            }
                                        }
                                        if (voargs.Count > 0)
                                        {
                                            this.AddPath(pathargs: voargs.ToArray());
                                            voargs.Clear();
                                        }
                                        voargs = null;
                                    }
                                }
                                else if (kv.Value is List<string> || kv.Value is string[])
                                {
                                    var voarr = (kv.Value is List<string>) ? ((List<string>)kv.Value).ToArray() : (string[])kv.Value;
                                    this.AddPath(pathargs: voarr);
                                }
                            }
                        }
                    }
                }
            }
        }

        public virtual string[] RequestHeaders=>null;

        public virtual void SetRequestHeader(string header, params string[] value)
        {
        }

        public virtual string RequestHeader(string header)
        {
            return "";
        }

        public virtual string[] ResponseHeaders => null;

        public virtual string ResponseHeader(string header)
        {
            return "";
        }

        public virtual void SetResponseHeader(string header, params string[] value)
        {
        }

            public Smpp.SmppMS SMPP()
        {
            return Smpp.SmppMS.SMPP();
        }

        public Scheduling.Schedules SCHEDULES()
        {
            return Scheduling.Schedules.SCHEDULES();
        }

        public Environment.Environment ENV()
        {
            return Environment.Environment.ENV();
        }

        public void ReloadEnvConfig()
        {
            Lnksnk.Core.Net.Endpoints.LoadConfig();
        }

        public void Listen(params string[] sports)
        {
            Endpoints.Listen(sports);
        }

        public Services SERVICES()
        {
            return Services.SERVICES();
        }

        public Data.Dbms DBMS()
        {
            return Data.Dbms.DBMS();
        }

        public DBConnection DBCN(string dbalias)
        {
            var dbcns = this.DBCNs(dbalias);
            if (dbcns.Length == 1)
            {
                return dbcns[0];
            }
            dbcns = null;
            return null;
        }

        public DBConnection[] DBCNs(params string[] dbaliasses)
        {
            if (dbaliasses == null || dbaliasses.Length == 0) return new DBConnection[0];
            List<DBConnection> dbcns = new List<DBConnection>();
            foreach (var dbalias in dbaliasses)
            {
                DBConnection dbcn = null;
                if (!dbalias.Trim().Equals(""))
                {
                    if (this.dbcntns != null && this.dbcntns.ContainsKey(dbalias.Trim()))
                    {
                        dbcn = this.dbcntns[dbalias.Trim()];
                    }
                    else
                    {
                        if ((dbcn = Data.Dbms.DBMS().DbConnection(dbalias.Trim())) != null)
                        {
                            (this.dbcntns == null ? (this.dbcntns = new Dictionary<string, DBConnection>()) : this.dbcntns).Add(dbalias.Trim(), dbcn);
                        }
                    }
                }
                if (dbcn != null)
                {
                    dbcns.Add(dbcn);
                }
            }
            if (dbcns.Count > 0)
            {
                var dbcnsfound = dbcns.ToArray();
                dbcns.Clear();
                dbcns = null;
                return dbcnsfound;
            }
            return emptyDBConnections;
        }

        private static DBConnection[] emptyDBConnections = new DBConnection[0];
        private bool disposedValue;

        public virtual string[] StandardParameters
        {
            get { return null; }
        }

        public virtual string[] StandardParameter(string key) {
            return null;
        }

        public IEnumerator<ResourceBase> GetEnumerator()
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (this.web != null)
                    {
                        this.web = null;
                    }
                    if (this.parameters != null)
                    {
                        this.parameters = null;
                    }
                    if (this.paths != null)
                    {
                        this.paths.Clear();
                        this.paths = null;
                    }
                    if (this.pathReader != null)
                    {
                        this.pathReader.Dispose();
                        this.pathReader = null;
                    }
                    if (this.activeMap != null)
                    {
                        this.activeMap.Clear();
                        this.activeMap = null;
                    }
                    if (this.pathsettings != null)
                    {
                        this.pathsettings.Clear();
                        this.pathsettings = null;
                    }
                    if (this.asmMap != null)
                    {
                        if (this.asmMap.Count > 0)
                        {
                            foreach (var key in this.asmMap.Keys.ToArray())
                            {
                                this.asmMap[key] = null;
                                this.asmMap.Remove(key);
                            }
                            this.asmMap.Clear();
                        }
                        this.asmMap = null;
                    }
                    if (this.dbcntns != null)
                    {
                        if (this.dbcntns.Count > 0)
                        {
                            foreach (var dbalias in this.dbcntns.Keys.ToArray())
                            {
                                this.dbcntns[dbalias] = null;
                                this.dbcntns.Remove(dbalias);
                            }
                            this.dbcntns.Clear();
                        }
                        this.dbcntns = null;
                    }
                }
                disposedValue = true;
            }
        }

        ~BaseHandler()
        {
            Dispose(!disposedValue);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private class SourceStreamReader : System.IO.StreamReader
        {
            private Action disposeAction = null;
            public SourceStreamReader(Action disposeAction,string path) : base(path) {
                this.disposeAction = disposeAction;
            }

            public SourceStreamReader(Action disposeAction, Stream stream, Encoding encoding) : base(stream, encoding) {
                this.disposeAction = disposeAction;
            }

            private bool disposingVal = false;
            protected override void Dispose(bool disposing)
            {
                if (!this.disposingVal)
                {
                    if(disposeAction!=null)
                    {
                        this.disposeAction();
                        this.disposeAction = null;
                    }
                }
                base.Dispose(disposing);
            }
        }

        public async Task<TextReader> FindActiveSourceAsync(string sourcePath, params object[] args)
        {
            TextReader sourceReader = null;

            await Task.Run(() => {

                var pths = (sourcePath.Equals("/") ? "" : sourcePath).Split("/");
                var pthsroot = "";
                var pthsrootzip = "";
                foreach (var ps in pths)
                {
                    if (pthsrootzip.Equals("") && !ps.Equals("") && (System.IO.File.Exists(this.RootPath() + (pthsroot.EndsWith("/") ? pthsroot : (pthsroot + "/")) + ps + ".zip")))
                    {
                        pthsrootzip = (pthsroot.EndsWith("/") ? pthsroot : (pthsroot + "/")) + ps + ".zip";
                        pthsroot = "";
                    }
                    else
                    {
                        pthsroot += (pthsroot.EndsWith("/") ? "" : "/") + ps;
                    }
                }

                if (pthsrootzip.Equals(""))
                {
                    if (Environment.Environment.ENV().ContainsTextReaderCall(pthsroot))
                    {
                        Func<TextReader> readercall = Environment.Environment.ENV().TextReaderCall(pthsroot);
                        sourceReader=readercall.Invoke();
                    }
                    else
                    {
                        if ((System.IO.File.Exists(this.RootPath() + pthsroot)))
                        {
                            sourceReader=new SourceStreamReader(null,this.RootPath() + pthsroot);
                        }
                    }
                }
                else if (!pthsrootzip.Equals("") && !pthsroot.Equals(""))
                {
                    if (System.IO.File.Exists(this.RootPath() + pthsrootzip))
                    {
                        var zip = ZipFile.OpenRead(this.RootPath() + pthsrootzip);
                        foreach (var zipe in zip.Entries)
                        {
                            if (zipe.FullName.Equals(pthsroot.Substring(1)))
                            {
                                System.IO.Stream tmpstrm = null;
                                sourceReader = new SourceStreamReader(() =>{
                                    if (tmpstrm != null)
                                    {
                                        tmpstrm.Close();
                                        tmpstrm = null;
                                    }
                                    if (zip != null) {
                                        zip.Dispose();
                                        zip = null;
                                    }
                                },tmpstrm = zipe.Open(), Encoding.UTF8);
                                break;
                            }
                        }
                        if (sourceReader == null)
                        {
                            zip.Dispose();
                            zip = null;
                        }
                    }
                }
            });
            return sourceReader;
        }

        private Dictionary<String, Assembly> assemblyMap = new Dictionary<string, Assembly>();

        public virtual Type FindActiveSourceType(string sourcePath, params object[] args)
        {
            var asmtypepath = sourcePath.Substring(0, sourcePath.Length - (sourcePath.Length - sourcePath.LastIndexOf(".")));
            asmtypepath = asmtypepath.Replace("/", ".");
            if (asmtypepath.StartsWith("."))
            {
                asmtypepath = asmtypepath.Substring(1);
            }
            var sourceAsm = this.LoadAsm(asmtypepath);
            if(sourceAsm!=null)
            {
                return this.LoadAsmType(sourceAsm, asmtypepath);
            }
            return null;
        }

        public object InvokeActiveSourceType(Type sourceType, params object[] args)
        {
            if (sourceType == null) return null;
            if (args==null||args.Length==0)
            {
                return sourceType.GetConstructor(new Type[] { }).Invoke(parameters:null);
            }
            foreach (var cnstrktr in sourceType.GetConstructors())
            {
                var prms = cnstrktr.GetParameters();
                
                if (prms != null&&prms.Length==args.Length)
                {
                    var prmsi = 0;
                    var argvals = new object[args.Length];
                    foreach (var prm in prms)
                    {
                        if(args[prmsi]==null)
                        {
                            argvals[prmsi] = (args[prmsi] == null) ? prm.HasDefaultValue ? prm.DefaultValue : null : args[prmsi];
                        } else if(args[prmsi].GetType().IsAssignableFrom(prm.ParameterType))
                        {
                            argvals[prmsi] = args[prmsi];
                        } else
                        {
                            break;
                        }
                        prmsi++;
                    }
                    if (prmsi==args.Length)
                    {
                        return cnstrktr.Invoke(argvals);
                    }
                    argvals = null;
                }
            }
            return null;
        }
    }
}
