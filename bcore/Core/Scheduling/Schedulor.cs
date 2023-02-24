using Lnksnk.Core.Data;
using Lnksnk.Core.Net;
using Lnksnk.Environment;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lnksnk.Core.Scheduling
{

    public class Schedulor : IDisposable
    {
        private bool disposedValue;

        private Schedules schdls;
        private String alias;

        public String Alias => this.alias;

        public Schedulor(Schedules schdls,String alias) {
            this.schdls = schdls;
            this.alias = alias;
        }

        private DateTime fromDate=DateTime.Today;
        public DateTime FromDate => this.fromDate;

        private DateTime toDate=DateTime.Today.AddDays(1);
        public DateTime ToDate => this.toDate;

        private int seconds = 10;

        public int Seconds {
            get => this.seconds;

            set {
                if (value < 10)
                {
                    this.seconds = 10;
                }
                else {
                    this.seconds = value;
                }
            }
        }

        public bool AddAction(String actionname, Action<Schedulor,String,DateTime, Object[]> action, params Object[] args) {
            bool addAction = false;
            lock (this.actnslck) {
                if (this.actionnames.Contains(actionname))
                {
                    addAction = false;
                }
                else {
                    this.actionnames.Add(actionname);
                    this.actions.Add(action);
                    if (args != null)
                    {
                        this.actionsparams.Add(args);
                    }
                    else {
                        this.actionsparams.Add(null);
                    }
                    addAction = true;
                }
            }
            return addAction;
        }

        private List<String> actionnames=new List<String>();

        private List<Object[]> actionsparams = new List<Object[]>();

        private List<Action<Schedulor, String, DateTime, Object[]>> actions = new List<Action<Schedulor, String, DateTime, Object[]>>();

        private readonly object actnslck = new object();
        private bool callinggc = false;
        private void Execute() {
            lock (actnslck) {
                var ic = actionnames.Count;
                if (actionnames.Count > 0 && actionnames.Count == actions.Count && actionnames.Count <= actionsparams.Count)
                {
                    while ((ic-=1)>=0)
                    {
                        actions[actions.Count-(ic+1)](this, actionnames[actions.Count - (ic + 1)], DateTime.Now, actionsparams[actions.Count- (ic + 1)]);
                    }
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (this.running)
                    {
                        this.Stop();
                    }
                    if (this.schdls != null)
                    {
                        lock (this.schdls.schdlors)
                        {
                            if (this.schdls.schdlors.ContainsKey(this.alias))
                            {
                                this.schdls.schdlors.Remove(this.alias);
                            }
                        }
                        this.schdls = null;
                    }
                    if (this.actionnames != null) {
                        this.actionnames.Clear();
                        this.actionnames = null;
                    }
                    if (this.actionsparams != null) {
                        this.actionsparams.Clear();
                    }
                    if (this.actions != null) {
                        this.actions.Clear();
                        this.actions = null;
                    }
                }

                disposedValue = true;
            }
        }

        private DateTime lastRunStamp=DateTime.Today;

        public bool Start() {
            return this.Startup();
        }

        private bool running = false;
        private void Run() {
            this.running = true;
            var didPulse = false;
            var needReset = false;
            TimeSpan nextPulseTime=TimeSpan.Zero;
            bool buzy = false;
            while (running) {
                TimeSpan elapsedTime = DateTime.Now.TimeOfDay - this.fromDate.TimeOfDay;
                TimeSpan maxTime = (this.toDate.AddSeconds(-1).TimeOfDay - this.fromDate.TimeOfDay);
                if ((int)elapsedTime.TotalSeconds == (int)maxTime.TotalSeconds) {
                    maxTime.Add(TimeSpan.FromSeconds((double)this.seconds));
                }
                if (elapsedTime < maxTime)
                {
                    if (needReset)
                    {
                        didPulse = false;
                        needReset = false;
                    }
                    int elapsedintervalsecs = ((int)(elapsedTime.TotalSeconds / this.seconds) * this.seconds);
                    if ((int)elapsedTime.TotalSeconds == elapsedintervalsecs)
                    {
                        if (!didPulse)
                        {
                            nextPulseTime = elapsedTime;
                            didPulse = true;
                        }
                        if ((int)nextPulseTime.TotalSeconds == (int)elapsedTime.TotalSeconds)
                        {
                            if (!buzy)
                            {
                                buzy = true;
                                Task.Run(() => {
                                    Execute();
                                    buzy = false;
                                });   
                            }
                        }
                        if (didPulse)
                        {
                            nextPulseTime = elapsedTime.Add(TimeSpan.FromSeconds((double)this.seconds));
                        }
                    }
                }
                else {
                    needReset = true;
                }
                Thread.Sleep(TimeSpan.FromMilliseconds(10));
            }
        }

        private Boolean started = false;
       
        private bool Startup() {
            if (started)
            {
                return false;
            }
            Task.Run(() => {
                Run();
            });
            while (!running) {
                Thread.Sleep(10);
            }
            started = running;
            return started;
        }

        private async Task<Boolean> Shutdown()
        {
            return await Task<Boolean>.Run(() => {
                if (started)
                {
                    return true;
                }
                return false;
            });
        }

        public bool Stop() {
            return this.Shutdown().Result;
        }

        ~Schedulor()
        {
            Dispose(!disposedValue);
        }

        public void Dispose()
        {
            Dispose(!disposedValue);
            GC.SuppressFinalize(this);
        }

        //List of actions
        public void AddActionDataExport(string actionname, string dbalias, String exportpath, String filename, string query,params object[] parameters)
        {
            this.AddAction(actionname, (schdl, actionname, execstamp, args) =>
            {
                exportpath = (exportpath == null || exportpath.Trim() == "" ? "" : exportpath.Trim()).Replace("\\", "/");
                var froot = Environment.Environment.ENV().Root;
                if (exportpath!="/"&&Directory.Exists(exportpath))
                {
                    froot = exportpath.Replace("\\", "/");
                }
                else
                {
                    if (froot.EndsWith("/"))
                    {
                        if (exportpath.StartsWith("/"))
                        {
                            froot = froot + exportpath.Substring(1);
                        }
                        else
                        {
                            froot = froot + exportpath;
                        }
                    }
                    else
                    {
                        froot = froot + exportpath;
                    }
                }
                if (Directory.Exists(froot))
                {
                    var fnametouse = filename.Trim();
                    var expext = ".csv";
                    var fnameisvalid = false;
                    if (!fnametouse.Equals(""))
                    {
                        if (fnametouse.LastIndexOf(".") > 0)
                        {
                            var fnametouseext = fnametouse.Substring(fnametouse.IndexOf(".")).Trim();
                            if (fnametouseext.Equals(".json") || fnametouseext.Equals(".csv"))
                            {
                                fnameisvalid = true;
                                expext = fnametouseext;
                            }

                            if (fnameisvalid)
                            {
                                if (!fnametouse.Substring(fnametouse.IndexOf(".") + 1).Trim().Equals(""))
                                {
                                    fnametouse = fnametouse.Substring(0, fnametouse.IndexOf(".")).Trim();
                                }
                                else
                                {
                                    fnameisvalid = false;
                                }
                            }
                        }
                        else
                        {
                            fnameisvalid = true;
                        }
                        if (fnameisvalid)
                        {
                            if (fnametouse.Contains("<timestamp")){
                                String timestampmask = fnametouse.Substring(fnametouse.IndexOf("<timestamp") + "<timestamp".Length);
                                if (timestampmask.IndexOf(">") >= 0) {
                                    int timestampl = timestampmask.IndexOf(">") + 1;
                                    timestampmask = timestampmask.Substring(0, timestampmask.IndexOf(">"));
                                    
                                    fnametouse = fnametouse.Substring(0, fnametouse.IndexOf("<timestamp")) +(timestampmask.Trim()==""?"": execstamp.ToString(timestampmask.Trim())) + fnametouse.Substring(fnametouse.IndexOf("<timestamp") + "<timestamp".Length + timestampl);
                                }
                            }
                            var fullextractname = froot + fnametouse + expext;
                            if (!File.Exists(fullextractname))
                            {
                                using (var dbreader = DbQuery(dbalias, query, args))
                                {
                                    if (dbreader != null)
                                    {
                                        if (expext.Equals(".csv"))
                                        {
                                            var strwrt = new StreamWriter(fullextractname);
                                            dbreader.WriteCSV(strwrt, true);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            },args:parameters);
        }

        public void AddActionDataQuery(string actionname, string dbalias, string query, Action<Data.DataReader,String> dataAction, params object[] parameters)
        {
            this.AddAction(actionname, (schdl, actionname, execstamp, args) =>
            {
                if (dataAction != null)
                {
                    using (var dbreader = DbQuery(dbalias, query, args))
                    {
                        if (dbreader != null)
                        {
                            dataAction(dbreader,dbalias);
                        }
                    }
                }
            },args: parameters);
        }

        public void AddActionBulkDataExecute(string actionname, string srcdbalias, string srcquery, string destdbalias, string destquery, params object[] parameters)
        {
            this.AddAction(actionname, (schdl, actionname, execstamp, args) =>
            {
                Data.Dbms.DBMS().DbBulkExecute(srcdbalias, srcquery, destdbalias, destquery, parameters: args);
            },args: parameters);
        }

        public void AddActionDbBulkExecuteAndWrapup(string actionname, string srcdbalias, string srcquery, string destdbalias, string destquery, string srcwrapupquery, params object[] parameters)
        {
            this.AddAction(actionname, (schdl, actionname, execstamp, args) =>
            {
                Data.Dbms.DBMS().DbBulkExecuteAndWrapup(srcdbalias, srcquery, destdbalias, destquery, srcwrapupquery, parameters: args);
            }, args: parameters);
        }

        public DataReader DbQuery(string dbalias, string query, params object[] parameters)
        {
            var datareader = Dbms.DBMS().DbQuery(dbalias, query, parameters);
            return datareader;
        }

        public void AddActionRequest(string actionname, string rspath, params object[] parameters)
        {
            this.AddAction(actionname, (schdl, actionname, execstamp, args) =>
            {
                Endpoints.ExecuteRequest(rspath, parameters: args);
            },args: parameters);
        }
    }
}
