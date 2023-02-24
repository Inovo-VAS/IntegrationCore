using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;

namespace chatparameters
{
    /// <summary>
    /// Summary description for Handler
    /// </summary>
    public class Handler : IHttpHandler
    {
        private System.Data.SqlClient.SqlClientFactory sqlcnfactory = System.Data.SqlClient.SqlClientFactory.Instance;

        private String formatColumnDisplay(string column)
        {
            column = column == null ? "" : column.Trim();
            if (column.IndexOf(":")>0&&!column.Substring(column.IndexOf(":")+1).Equals(""))
            {
                return column.Substring(column.IndexOf(":") + 1);
            }
            return column;
        }

        public void ProcessRequest(HttpContext context)
        {
            
            var configpath = "";
            var dbcommand = "";
            var canQuery = false;
            var singleQuery = false;
            var chatparams = new Dictionary<string, List<string>>();
            var request = context.Request;
            var response = context.Response;
            var accesstokenref = "";
            var userhostaddress = request.UserHostAddress;
            chatparams.Add("USERHOSTADDRESS", new List<string>());
            chatparams["USERHOSTADDRESS"].Add(userhostaddress);
            var userhostname = request.UserHostName;
            chatparams.Add("USERHOSTNAME", new List<string>());
            chatparams["USERHOSTNAME"].Add(userhostname);
            if (request.QueryString != null && request.QueryString.Count > 0) {
                foreach (var prmk in request.QueryString.Keys) {
                    if (prmk == null) continue;
                    if (configpath.Equals("") && ((string)prmk).ToLower().Equals("configpath"))
                    {
                        configpath = request.QueryString[(string)prmk];
                    } else if (((string)prmk).ToLower().Equals("accesstoken")&& accesstokenref.Equals(""))
                    {
                        accesstokenref = request.QueryString[(string)prmk].Trim();
                    }
                    else
                    {
                        if (chatparams.ContainsKey((string)prmk))
                        {
                            chatparams[(string)prmk].Add(request.QueryString[(string)prmk]);
                        }
                        else
                        {
                            chatparams.Add((string)prmk, new List<string>());
                            chatparams[(string)prmk].Add(request.QueryString[(string)prmk]);
                        }
                    }
                }
            }
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.ContentType = "application/json";

            if (configpath==null || configpath.Equals(""))
            {
                configpath = ConfigurationManager.AppSettings.Get("defaultconfigsection");
            }

            NameValueCollection settings = configpath.Equals("")? null:(ConfigurationManager.GetSection(configpath) as System.Collections.Specialized.NameValueCollection);
            if (settings != null)
            {
                var accesstoken = "";
                try
                {
                    accesstoken = settings["accesstoken"];
                }
                catch (Exception)
                {
                }
                if (accesstoken==null)
                {
                    accesstoken = "";
                }
                if (accesstoken!=null && !accesstoken.Equals(""))
                {
                    accesstoken = accesstoken.Trim();
                }
                if (accesstokenref!=null && !accesstokenref.Equals(""))
                {
                    accesstokenref = accesstokenref.Trim();
                }

                if (accesstokenref.Equals(""))
                {
                    configpath = ConfigurationManager.AppSettings.Get("defaultaccesstoken");
                }
                if (accesstokenref != null && !accesstokenref.Equals(""))
                {
                    accesstokenref = accesstokenref.Trim();
                }

                JavaScriptSerializer jsrl = new JavaScriptSerializer();
                    var strm = new System.IO.StreamWriter(response.OutputStream);
                if (chatparams.Count > 0)
                {
                    try
                    {
                        System.Data.Common.DbConnection sqlcn = sqlcnfactory.CreateConnection();
                        var dbconnection = "chatparameters";
                        try
                        {
                            dbconnection = settings["dbconnection"];
                        }
                        catch (Exception)
                        {
                        }
                        sqlcn.ConnectionString = ConfigurationManager.ConnectionStrings[dbconnection.Equals("") || dbconnection == null ? "chatparameters" : dbconnection].ConnectionString;
                        var schema = "";
                        var table = "";
                        try
                        {
                            schema = settings["dbschema"];
                        }
                        catch (Exception)
                        {
                        }

                        try
                        {
                            table = settings["dbtable"];
                        }
                        catch (Exception)
                        {
                        }
                        if (table == null || table.Equals(""))
                        {
                            try
                            {
                                dbcommand = settings["dbcommand"];
                                if (dbcommand == null || dbcommand.Equals(""))
                                {
                                    if ((dbcommand = settings["dbquery"]) != null && !dbcommand.Equals(""))
                                    {
                                        canQuery = true;
                                    }
                                    else if ((dbcommand = settings["dbsinglequery"]) != null && !dbcommand.Equals(""))
                                    {
                                        canQuery = true;
                                        singleQuery = true;
                                    }

                                }
                            }
                            catch (Exception)
                            {
                            }
                        }
                        sqlcn.Open();
                        if (accesstoken.Equals(accesstokenref))
                        {
                            var sqlcmd = sqlcn.CreateCommand();

                            if (table != null && !table.Equals(""))
                            {
                                sqlcmd.CommandText = "SELECT TOP 1 * FROM " + (schema.Equals("") ? "PTOOLS" : schema) + "." + (table.Equals("") || table == null ? "CHATURLSESSIONVARIABLES" : table);

                                var rdr = sqlcmd.ExecuteReader();
                                var validColNames = new List<string>();
                                var rcl = rdr.FieldCount;

                                while (validColNames.Count < rcl)
                                {
                                    validColNames.Add(rdr.GetName(validColNames.Count).ToUpper());
                                }

                                rdr.Close();

                                var colnames = "";
                                var paramnames = "";
                                foreach (var k in chatparams.Keys)
                                {
                                    var prm = sqlcmd.CreateParameter();
                                    var cname = (k.Equals("CLIENTTYPE") ? "CLIENTNUMBER" : k.Equals("SERVICE") ? "SERVICEID" : k).ToUpper();
                                    if (validColNames.Contains(cname))
                                    {
                                        colnames += cname + ",";
                                        paramnames += "@" + cname + ",";
                                        prm.ParameterName = cname;
                                        prm.Value = string.Join("", chatparams[k].ToArray());
                                        sqlcmd.Parameters.Add(prm);
                                    }
                                }
                                try
                                {
                                    sqlcmd.CommandText = "INSERT INTO " + (schema.Equals("") ? "PTOOLS" : schema) + ".CHATURLSESSIONVARIABLES (" + (colnames.EndsWith(",") ? colnames.Substring(0, colnames.Length - 1) : colnames) + ") SELECT " + (paramnames.EndsWith(",") ? paramnames.Substring(0, paramnames.Length - 1) : paramnames) + " WHERE NOT EXISTS (SELECT TOP 1 1 FROM " + (schema.Equals("") ? "PTOOLS" : schema) + ".CHATURLSESSIONVARIABLES WHERE SESSIONID=@SESSIONID)";
                                    sqlcmd.ExecuteNonQuery();
                                }
                                catch (Exception e)
                                {
                                    response.Write("SQL:" + "INSERT INTO " + (schema.Equals("") ? "PTOOLS" : schema) + ".CHATURLSESSIONVARIABLES (" + (colnames.EndsWith(",") ? colnames.Substring(0, colnames.Length - 1) : colnames) + ") SELECT " + (paramnames.EndsWith(",") ? paramnames.Substring(0, paramnames.Length - 1) : paramnames) + " WHERE NOT EXISTS (SELECT TOP 1 1 FROM " + (schema.Equals("") ? "PTOOLS" : schema) + ".CHATURLSESSIONVARIABLES WHERE SESSIONID=@SESSIONID)");
                                    response.Write("ERR:" + e.Message);
                                }
                            }
                            else if (dbcommand != null && !dbcommand.Equals(""))
                            {
                                var colnames = "";
                                var paramnames = "";
                                foreach (var k in chatparams.Keys)
                                {
                                    var prm = sqlcmd.CreateParameter();
                                    var cname = (k.Equals("CLIENTTYPE") ? "CLIENTNUMBER" : k.Equals("SERVICE") ? "SERVICEID" : k).ToUpper();
                                    prm.ParameterName = cname;
                                    prm.Value = string.Join("", chatparams[k].ToArray());
                                    sqlcmd.Parameters.Add(prm);
                                }
                                try
                                {
                                    sqlcmd.CommandText = dbcommand;
                                    if (canQuery)
                                    {
                                        var dbreader = sqlcmd.ExecuteReader();
                                        var cols = new List<String>();

                                        var colsi = 0;
                                        for (; colsi < dbreader.FieldCount;)
                                        {
                                            cols.Add(dbreader.GetName(colsi));
                                            colsi++;
                                        }
                                        if (!singleQuery)
                                        {
                                            strm.Write("{\"columns\":");
                                            colsi = 0;
                                            strm.Write("[");
                                            foreach (var col in cols)
                                            {
                                                strm.Write(jsrl.Serialize(formatColumnDisplay(col)));
                                                colsi++;
                                                if (colsi < cols.Count)
                                                {
                                                    strm.Write(",");
                                                }
                                            }
                                            strm.Write("]");

                                            var hasMore = dbreader.Read();
                                            if (hasMore)
                                            {
                                                strm.Write(",\"data\":[");
                                                Object[] data = new object[cols.Count];


                                                while (hasMore)
                                                {
                                                    dbreader.GetValues(data);
                                                    colsi = 0;
                                                    strm.Write("[");
                                                    foreach (var col in data)
                                                    {
                                                        if (col == null)
                                                        {
                                                            strm.Write("null");
                                                        }
                                                        else if (col is String)
                                                        {
                                                            strm.Write(jsrl.Serialize(col));
                                                        }
                                                        else if (col is DateTime)
                                                        {
                                                            strm.Write(jsrl.Serialize(((DateTime)col).ToString("yyyy-MM-dd HH:mm:ss.fff").Replace(" ", "T")));
                                                        }
                                                        else
                                                        {
                                                            strm.Write(jsrl.Serialize(col));
                                                        }

                                                        colsi++;
                                                        if (colsi < data.Length)
                                                        {
                                                            strm.Write(",");
                                                        }
                                                    }
                                                    strm.Write("]");
                                                    if (hasMore = dbreader.Read())
                                                    {
                                                        strm.Write(",");
                                                    }
                                                }
                                                strm.Write("]");
                                            }
                                            strm.Write("}");
                                        }
                                        else
                                        {
                                            var hasMore = dbreader.Read();
                                            var dbdatasections = settings.Get("dbdatasections");
                                            if (hasMore)
                                            {
                                                Object[] data = new object[cols.Count];
                                                dbreader.GetValues(data);

                                                if (dbdatasections == null || dbdatasections.Equals(""))
                                                {
                                                    strm.Write("{");
                                                    colsi = 0;
                                                    foreach (var col in data)
                                                    {
                                                        strm.Write(jsrl.Serialize(formatColumnDisplay(cols[colsi])) + ":");
                                                        if (col == null)
                                                        {
                                                            strm.Write("null");
                                                        }
                                                        else if (col is String)
                                                        {
                                                            strm.Write(jsrl.Serialize(col));
                                                        }
                                                        else if (col is DateTime)
                                                        {
                                                            strm.Write(jsrl.Serialize(((DateTime)col).ToString("yyyy-MM-dd HH:mm:ss.fff").Replace(" ", "T")));
                                                        }
                                                        else
                                                        {
                                                            strm.Write(jsrl.Serialize(col));
                                                        }

                                                        colsi++;
                                                        if (colsi < data.Length)
                                                        {
                                                            strm.Write(",");
                                                        }
                                                    }
                                                    strm.Write("}");
                                                }
                                                else
                                                {
                                                    var arrdbdatasections = dbdatasections.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                                                    var arrdbsi = 0;
                                                    strm.Write("{");
                                                    foreach (var dbsection in arrdbdatasections)
                                                    {
                                                        if (dbsection.IndexOf("=") > 0)
                                                        {
                                                            strm.Write(jsrl.Serialize(formatColumnDisplay(dbsection.Substring(0, dbsection.IndexOf("=")))) + ":");
                                                            var arrcolnms = dbsection.Substring(dbsection.IndexOf("=") + 1).Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                                                            var arrcolmnsi = 0;
                                                            strm.Write("{");
                                                            foreach (var arrcol in arrcolnms)
                                                            {
                                                                if ((colsi = cols.IndexOf(arrcol)) > -1)
                                                                {
                                                                    strm.Write(jsrl.Serialize(formatColumnDisplay(cols[colsi])) + ":");
                                                                    var col = data[colsi];
                                                                    if (col == null)
                                                                    {
                                                                        strm.Write("null");
                                                                    }
                                                                    else if (col is String)
                                                                    {
                                                                        strm.Write(jsrl.Serialize(col));
                                                                    }
                                                                    else if (col is DateTime)
                                                                    {
                                                                        strm.Write(jsrl.Serialize(((DateTime)col).ToString("yyyy-MM-dd HH:mm:ss.fff").Replace(" ", "T")));
                                                                    }
                                                                    else
                                                                    {
                                                                        strm.Write(jsrl.Serialize(col));
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    strm.Write("{" + arrcol + ":null}");
                                                                }
                                                                arrcolmnsi++;
                                                                if (arrcolmnsi < arrcolnms.Length)
                                                                {
                                                                    strm.Write(",");
                                                                }
                                                            }
                                                            strm.Write("}");
                                                        }
                                                        else
                                                        {
                                                            strm.Write(jsrl.Serialize(formatColumnDisplay(dbsection)) + ":null");
                                                        }
                                                        arrdbsi++;
                                                        if (arrdbsi < arrdbdatasections.Length)
                                                        {
                                                            strm.Write(",");
                                                        }
                                                    }
                                                    strm.Write("}");
                                                }
                                            } else
                                            {
                                                strm.Write("{}");
                                            }
                                        }
                                        try
                                        {

                                        }
                                        catch (Exception dbre)
                                        {

                                        }
                                        finally
                                        {
                                            try
                                            {
                                                if (dbreader != null)
                                                {
                                                    dbreader.Close();
                                                }
                                            }
                                            catch { }
                                        }
                                    }
                                    else
                                    {
                                        sqlcmd.ExecuteNonQuery();
                                        strm.Write(@"{""status"":""success""}");
                                    }
                                }
                                catch (Exception e)
                                {
                                    strm.Write(@"{""status"":""failed"",");
                                    strm.Write(@"""SQL"":" + jsrl.Serialize(dbcommand) + "\",");
                                    strm.Write(@"""ERR"":" + jsrl.Serialize(e.Message) + "\"}");
                                }
                            }
                            try { sqlcmd.Dispose(); } catch { }
                        } else
                        {
                            throw new Exception("Invalid AccessToken");
                        }
                        try { sqlcn.Close(); } catch { }
                    }
                    catch (Exception ex)
                    {
                        if (dbcommand != null)
                        {
                            strm.Write(@"{""status"":""failed"",");
                            strm.Write(@"""ERR"":" + jsrl.Serialize(ex.Message) + "}");
                        }
                        else
                        {
                            strm.Write("ERR\":" + ex.Message);
                        }
                    }
                }
                strm.Flush();
            } else
            {

            }
        }

        public bool IsReusable
        {
            get
            {
                return true;
            }
        }
    }
}