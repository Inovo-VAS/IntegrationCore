using Lnksnk.Core.Data;
using Lnksnk.Core.IO;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lnksnk.Core.Net.Web
{
    public class RequestHandler : BaseHandler
    {
        private Dictionary<string, Object> requestmap = new Dictionary<string, object>();

        public Dictionary<string, object> Map => this.requestmap;

        private HttpRequest asphttprequest = null;
        private HttpResponse asphttpresponse = null;

        internal RequestHandler(Web web, HttpRequest asphttprequest,HttpResponse asphttpresponse):base(web) {
            if ((this.asphttprequest = asphttprequest) != null) {
                if (asphttprequest.HttpContext.Connection != null && asphttprequest.HttpContext.Connection.RemoteIpAddress != null)
                {
                    this.userHostAddress = asphttprequest.HttpContext.Connection.RemoteIpAddress.ToString();
                }
            }
            this.asphttpresponse = asphttpresponse;
            this.activeMap.Add("request", this);
        }

        internal void Execute(HttpRequest request, HttpResponse response)
        {            
            this.ExecuteHandler(request, response,doneReading: () =>
            {
                if (this.Web != null)
                {
                    this.Web.WriteResponseHeader(response,request,this);
                }
            },doneReadingNothing: () =>
            {
                if (this.Web != null)
                {
                    this.Web.WriteResponseHeader(response,request,null);
                }
            });
            
        }

        public override string[] StandardParameters {
            get {
                var keys = new List<string>();
                if (this.asphttprequest != null)
                {
                    if (this.asphttprequest.Query.Count > 0)
                    {
                        foreach (var k in this.asphttprequest.Query.Keys)
                        {
                            if (keys.Contains(k)) continue;
                            keys.Add(k);
                        }
                    }
                    try
                    {
                        if (this.asphttprequest.HasFormContentType && this.asphttprequest.Form.Count > 0)
                        {
                            foreach (var k in this.asphttprequest.Form.Keys)
                            {
                                if (keys.Contains(k)) continue;
                                keys.Add(k);
                            }
                        }
                    }
                    catch (Exception){ }
                }
                return keys.ToArray();
            }
        }

        public override string[] StandardParameter(string key)
        {
            if (this.asphttprequest != null)
            {
                var vals = new List<string>();
                if (this.asphttprequest != null)
                {
                    if (this.asphttprequest.Query.ContainsKey(key))
                    {
                        foreach(var v in this.asphttprequest.Query[key].ToArray())
                        {
                            vals.Add(v);
                        }
                    }
                    try
                    {
                        if (this.asphttprequest.HasFormContentType && this.asphttprequest.Form.ContainsKey(key))
                        {
                            foreach (var v in this.asphttprequest.Form[key].ToArray())
                            {
                                vals.Add(v);
                            }
                        }
                    }
                    catch (Exception) { }
                }
                return vals.ToArray();
            }
            else
            {
                return new string[0];
            }
        }

        public void BulkDataExecute(params object[] parameters)
        {
            if (this.asphttprequest != null)
            {
                var args = (object[])null;
                ActiveReader.PrepActiveArgs(out args, parameters);
                parameters = args;
                if (parameters!=null && parameters.Length>=4)
                {
                    string srcdbalias = (string)parameters[0];
                    string srcquery = (string)parameters[1];
                    string destdbalias = (string)parameters[2];
                    string destquery = (string)parameters[3];
                    parameters = parameters[4..];
                    DBMS().DbBulkExecute(srcdbalias, srcquery, destdbalias, destquery, parameters: parameters);
                }
            }
        }

        public void BulkDataExecuteAndWrapup(params object[] parameters)
        {
            if (this.asphttprequest != null)
            {
                var args = (object[])null;
                ActiveReader.PrepActiveArgs(out args, parameters);
                parameters = args;
                if (parameters != null && parameters.Length >= 5)
                {
                    string srcdbalias = (string)parameters[0];
                    string srcquery = (string)parameters[1];
                    string destdbalias = (string)parameters[2];
                    string destquery = (string)parameters[3];
                    string srcwrapupquery = (string)parameters[4];
                    parameters = parameters[5..];
                    DBMS().DbBulkExecuteAndWrapup(srcdbalias, srcquery, destdbalias, destquery,srcwrapupquery, parameters: parameters);
                }
            }
        }

        public override DataReader DataReader(params object[] parameters)
        {
            return base.DataReader(this.asphttprequest,parameters: parameters);
        }

        private bool disposedValue = false;

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    base.Dispose(disposedValue);
                    if (this.asphttprequest != null) {
                        this.asphttprequest = null;
                    }
                    if (this.requestmap != null) {
                        if (this.requestmap.Count > 0) {
                            foreach (var rqstmk in this.requestmap.Keys.ToArray()) {
                                this.requestmap[rqstmk] = null;
                                this.requestmap.Remove(rqstmk);
                            }
                            this.requestmap.Clear();
                        }
                        this.requestmap = null;
                    }
                }
                disposedValue = true;
            }
        }

        ~RequestHandler()
        {
            Dispose(!disposedValue);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public override string[] RequestHeaders {
            get {
                string[] rqesthdrs = null;
                if (this.asphttprequest != null)
                {
                    rqesthdrs=this.asphttprequest.Headers.Keys.ToArray();
                }
                return rqesthdrs == null ? new string[0] : rqesthdrs;
            }
        }

        public override string RequestHeader(string header)
        {
            if (this.asphttprequest != null && this.asphttprequest.Headers.ContainsKey(header))
            {
                return this.asphttprequest.Headers[header];
            }
            return "";
        }

        public override string ResponseHeader(string header) {
            if (this.asphttpresponse != null && this.asphttpresponse.Headers.ContainsKey(header)) {
                return this.asphttpresponse.Headers[header];
            }
            return "";
        }

        public override void SetResponseHeader(string header, params string[] value) {
            if (this.asphttpresponse != null) {
                if (value != null && value.Length > 0)
                {
                    if (this.asphttpresponse.Headers.ContainsKey(header))
                    {

                        this.asphttpresponse.Headers[header] = new Microsoft.Extensions.Primitives.StringValues(value);
                    }
                    else {
                        this.asphttpresponse.Headers.Add(header, new Microsoft.Extensions.Primitives.StringValues(value));
                    }
                }
            }
        }

        public override string[] ResponseHeaders
        {
            get
            {
                string[] rspnshdrs = null;
                if (this.asphttpresponse != null)
                {
                    rspnshdrs = this.asphttpresponse.Headers.Keys.ToArray();
                }
                return rspnshdrs == null ? new string[0] : rspnshdrs;
            }
        }
    }
}
