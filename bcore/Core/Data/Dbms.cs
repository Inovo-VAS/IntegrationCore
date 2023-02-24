using System;
using System.Collections.Generic;
using System.Text;

namespace Lnksnk.Core.Data
{
    internal delegate DataReader DbQuerying(string dbalias, string query, params object[] parameters);
    internal delegate bool RegisteringDbConnection(string dbalias, string factoryassembly, string cnstring);
    public class Dbms
    {
        private static Dbms dbms=new Dbms();
        private Dictionary<String, DBConnection> dbcndictionary = new Dictionary<string, DBConnection>();

        public DataReader DbQuery(string dbalias, string query,params object[] parameters)
        {
            var datareader = dbcndictionary.ContainsKey(dbalias) ? dbcndictionary[dbalias].Query(query.Replace('\0', '@'), parameters:parameters) : null;
            return datareader;
        }

        public DBConnection DbConnection(string dbalias) {
            DBConnection dbcn = null;
            if (dbalias != null && !(dbalias = dbalias.Trim()).Equals("")) {
                lock (dbcndictionary) {
                    if (dbcndictionary.ContainsKey(dbalias)){
                        dbcn = dbcndictionary[dbalias];
                    }
                }
            }
            return dbcn;
        }

        public DataExecutor DbExecutor(string dbalias)
        {
            if (dbcndictionary.ContainsKey(dbalias)) {
                return dbcndictionary[dbalias].Executor();
            } 
            return null;
        }

        public bool RegisterDbConnection(string dbalias, string factoryassembly, string cnstring,string sqlprmpprefix="")
        {
            Logging.Log.LOG().Info("RegisterDbConnection:" + dbalias);
            var didRegister = false;
            lock (dbcndictionary)
            {
                if (!dbcndictionary.ContainsKey(dbalias))
                {
                    if ((factoryassembly == "Remote") || (factoryassembly == "SqlServer") || (factoryassembly == "Microsoft.Data.SqlClient") || (factoryassembly == "PostgreSql") || (factoryassembly == "Npgsql") || (factoryassembly == "MySql") || (factoryassembly == "MySql.Data") || (factoryassembly == "Oracle.ManagedDataAccess.Client") || (factoryassembly == "Oracle"))
                    {
                        if (factoryassembly == "SqlServer")
                        {   
                            factoryassembly = "Microsoft.Data.SqlClient.SqlClientFactory";
                            sqlprmpprefix = "@";
                        }
                        else if (factoryassembly == "MySql")
                        {
                            factoryassembly = "MySql.Data.MySqlClient.MySqlClientFactory";
                            sqlprmpprefix = "?";
                        }
                        else if (factoryassembly == "Oracle")
                        {
                            factoryassembly = "Oracle.ManagedDataAccess.Client.OracleClientFactory";
                            sqlprmpprefix = "?";
                        }
                        var dbcn = new DBConnection(factoryassembly, cnstring,qryprmsql:sqlprmpprefix);
                        dbcndictionary[dbalias] = dbcn;
                        didRegister = true;
                        Logging.Log.LOG().Info("RegisterDbConnection:registered->" + dbalias);
                    }
                    else
                    {
                        Logging.Log.LOG().Info("RegisterDbConnection:not registered->" + dbalias);
                        didRegister = false;
                    }
                }
                else
                {
                    Logging.Log.LOG().Info("RegisterDbConnection:already registered->" + dbalias);
                    didRegister = true;
                }
            }
            return didRegister;
        }

        public void DbBulkExecute(string srcdbalias, string srcquery, string destdbalias, string destquery, params object[] parameters)
        {
            this.DbBulkExecuteAndWrapup(srcdbalias, srcquery, destdbalias, destquery, "", parameters: parameters);
        }

        public void DbBulkExecuteAndWrapup(string srcdbalias, string srcquery,string destdbalias,string destquery,string srcwrapupquery, params object[] parameters)
        {
            var srcrecs = this.DbQuery(srcdbalias, srcquery, parameters);
            var destexectr = this.DbExecutor(destdbalias);
            var altparenthesis = "";
            var srcwrapupexectr=srcwrapupquery==null||(srcwrapupquery=srcwrapupquery.Trim()).Equals("")?null:this.DbExecutor(srcdbalias);

            if (parameters!=null&& parameters.Length>=1 && parameters[0] is string)
            {
                altparenthesis = (string)parameters[0];
                parameters = parameters[1..];
            }

            foreach (var rec in srcrecs)
            {
                
                if (altparenthesis == "")
                {
                    destexectr.Execute(destquery, rec);
                }
                else
                {
                    destexectr.Execute(destquery, altparenthesis, rec);
                }
                if (srcwrapupexectr != null) {
                    if (altparenthesis == "")
                    {
                        srcwrapupexectr.Execute(srcwrapupquery, rec);
                    }
                    else
                    {
                        srcwrapupexectr.Execute(srcwrapupquery, altparenthesis, rec);
                    }
                }
            }
        }

        public static Dbms DBMS() {
            return dbms;
        }
    }
}
