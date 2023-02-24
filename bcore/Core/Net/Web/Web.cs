using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections;
using System.Buffers;
using Lnksnk.Core.IO;
using System.Net;

namespace Lnksnk.Core.Net.Web
{
    delegate void RequestingResponding(HttpRequest request, HttpResponse response);

    public class Web :IDisposable
    {
        internal Web() {

        }

        internal static void WebRemoteRequest(HttpWebRequest request, HttpWebResponse response, System.Threading.CancellationToken cancel, bool isWebSocket = false)
        {
            using (var web = new Web())
            {
                web.RemoteRequestingResponding(request, response, cancel);
            }
        }

        internal static void WebRequest(HttpRequest request, HttpResponse response, System.Threading.CancellationToken cancel, bool isWebSocket = false)
        {
            using (var web = new Web())
            {
                web.RequestingResponding(request, response, cancel);
            }
        }
        internal static void WebSocketRequest(WebSocket socket, System.Threading.CancellationToken cancel)
        {

        }

        private bool readRequest = true;

        private string requestMimeType = "text/plain";
        internal System.Threading.CancellationToken cancel;

        public void RemoteRequestingResponding(HttpWebRequest request, HttpWebResponse response, System.Threading.CancellationToken cancel)
        {
            this.cancel = cancel;
            var path = request.Address.LocalPath;
            this.requestMimeType = Mimetypes.FindExtMimetype(path);
            using (var httpResponseHandler = new ResponseHandler(this, request, response))
            {
                httpResponseHandler.AddPath(path.Equals("") ? "/" : (path.IndexOf("?") == -1 ? path : path.Substring(0, path.IndexOf("?"))));
                httpResponseHandler.Execute(request, response);
            }
        }

        public void RequestingResponding(HttpRequest request, HttpResponse response, System.Threading.CancellationToken cancel)
        {
            this.cancel = cancel;
            var path = request.Path.Value;
            this.requestMimeType = Mimetypes.FindExtMimetype(path);
            using (var httpRequestHandler = new RequestHandler(this, request,response))
            {
                httpRequestHandler.AddPath(path.Equals("") ? "/" : (path.IndexOf("?")==-1?path:path.Substring(0,path.IndexOf("?"))));
                httpRequestHandler.Execute(request,response);
            }
        }

        internal void WriteResponseHeader(HttpResponse response, HttpRequest request,RequestHandler rqsthndlr=null)
        {
            if (this.readRequest)
            {
                try
                {
                    response.ContentType = this.requestMimeType.Replace("text/csv","text/plain");
                    if (this.requestMimeType.StartsWith("video/"))
                    {
                        if (!response.Headers.ContainsKey("Accept-Ranges")) {
                            response.Headers.Add("Accept-Ranges", "bytes");
                        }
                        if (rqsthndlr != null) { 
                            if (rqsthndlr.lastStartOffset > -1)
                            {
                                if(!response.Headers.ContainsKey("Content-Range")){
                                    var rslen = rqsthndlr.Current.ResourceLength;
                                    if (rslen>0)
                                    {
                                        response.Headers.Add("Content-Range","bytes " + rqsthndlr.lastStartOffset.ToString() + "-" + (rslen - 1).ToString() + "/" + rslen.ToString());
                                    }
                                    response.StatusCode = 206;
                                }
                            }
                        }
                    }
                }
                catch (Exception) {
                }
                this.readRequest = false;
            }
        }

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    
                }
                disposedValue = true;
            }
        }

        ~Web()
        {
            Dispose(!disposedValue);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
