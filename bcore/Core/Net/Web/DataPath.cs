using Lnksnk.Core.Data;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lnksnk.Core.Net.Web
{
    public class DataPath : ResourcePath
    {
        private DBConnection dbcn = null;
        private string dbalias = "";
        private string dbext = "";
        private string dbvaldelim = "";
        private string dbtxtpar = "";
        private string dbrowdelim = "";

        private bool js = false;
        private bool json = false;
        private bool xml = false;
        private bool csv = false;

        internal DataPath(BaseHandler bsehndlr) : base(bsehndlr)
        {
        }

        internal override bool PrepNextPath(string path, string mimetype, bool firstPath, bool lastPath, bool textContent, Dictionary<string, object> pathsettings)
        {
            if ((this.js= path.EndsWith(".js")) || (this.xml=path.EndsWith(".xml")) || (this.json=path.EndsWith(".json")) || (this.csv=path.EndsWith(".csv")))
            {
                if (path.StartsWith("/dbms-"))
                {
                    if (path.Substring("/dbms-".Length).IndexOf("/") > 0)
                    {
                        var psbldbalias = path.Substring("/dbms-".Length).Substring(0, path.Substring("/dbms-".Length).IndexOf("/"));
                        if (!psbldbalias.Equals(""))
                        {
                            if ((this.dbcn = this.RequestHandler.DBMS().DbConnection(psbldbalias)) != null)
                            {
                                this.dbalias = psbldbalias;
                                this.dbext = path.Substring(path.LastIndexOf("."));
                                path = path.Substring("/dbms-".Length).Substring(path.Substring("/dbms-".Length).IndexOf("/"));
                            }
                        }
                    }
                }
            }
            if (base.PrepNextPath(path, mimetype, firstPath, lastPath, textContent, pathsettings)) {
                return true;
            }
            return false;
        }

        public override void DoneAddingInitialResourcePath(bool initialResourceAdded)
        {
            if (!initialResourceAdded) {
                if (this.dbcn != null) {
                    if (!this.dbalias.Equals(""))
                    {
                        var sqlststmnd = "";
                        var sqlparamsvals = new Dictionary<string, object>();
                        var isquery = false;
                        foreach (var stdprm in this.RequestHandler.StandardParameters)
                        {
                            if (stdprm.Equals(this.dbalias + "-execute") || (!isquery && (isquery = stdprm.Equals(this.dbalias + "-query"))))
                            {
                                sqlststmnd = String.Join("", value: this.RequestHandler.StandardParameter(stdprm));
                            }
                            else if (stdprm.StartsWith(this.dbalias + ":"))
                            {
                                var prmname = stdprm.Substring((this.dbalias + ":").Length).Trim();
                                if (!prmname.Equals(""))
                                {
                                    var prmval = this.RequestHandler.StandardParameter(stdprm);
                                    if (prmval.Length > 1)
                                    {
                                    }
                                    else
                                    {
                                        sqlparamsvals.Add(prmname, prmval[0]);
                                    }
                                }
                            }
                            else if (this.csv) {
                                if (stdprm.Equals(this.dbalias + "-valdelim")){
                                    this.dbvaldelim = Strings.Join(this.RequestHandler.StandardParameter(stdprm), "");
                                } else if (stdprm.Equals(this.dbalias + "-rowdelim"))
                                {
                                    this.dbrowdelim = Strings.Join(this.RequestHandler.StandardParameter(stdprm), "");
                                }
                                else if (stdprm.Equals(this.dbalias + "-txtpar"))
                                {
                                    this.dbtxtpar = Strings.Join(this.RequestHandler.StandardParameter(stdprm), "");
                                }
                            }
                        }
                        if (!sqlststmnd.Equals(""))
                        {
                            DataReader dataReader = null;
                            DataExecutor dataExecutor = null;
                            if (isquery)
                            {
                                dataReader = this.dbcn.Query(sqlststmnd,parameters: sqlparamsvals);
                            } else
                            {
                                dataExecutor = this.dbcn.Executor();
                            }
                            if (this.json || this.js)
                            {
                                if (dataReader != null)
                                {
                                    this.AddNextReadingSource(new Reader(dataReader: dataReader, json: true));
                                }
                                else if (dataExecutor != null)
                                {
                                    if (sqlparamsvals == null || sqlparamsvals.Count == 0)
                                    {
                                        this.AddNextReadingSource(new Reader(dataExecutor: dataExecutor,query: sqlststmnd, json: true));
                                    } else
                                    {
                                        this.AddNextReadingSource(new Reader(dataExecutor: dataExecutor,query: sqlststmnd, json: true,sqlparamsvals));
                                    }
                                }
                            }
                            else if (this.csv)
                            {
                                if (dataReader != null)
                                {
                                    this.AddNextReadingSource(new Reader(dataReader: dataReader, csv: true,valdelim:this.dbvaldelim,txtpar:this.dbtxtpar,rowdelim:this.dbrowdelim));
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void DisposingResourcePath()
        {
            if (this.dbcn != null) {
                this.dbcn = null;
            }
        }
    }
}
