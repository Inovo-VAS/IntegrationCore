using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Configuration;
using System.Web.UI.WebControls;
using System.IO;

namespace LnksnkBroker
{
    /// <summary>
    /// Summary description for LnksnkHandler
    /// </summary>
    public class LnksnkHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            var endpointurl = ConfigurationSettings.AppSettings["endpointurl"];
            while (endpointurl.EndsWith("/")) {
                endpointurl = endpointurl.Substring(0, endpointurl.Length - 1);
            }
            var contextRequest = context.Request;
            
            var localurl = contextRequest.RawUrl;
            if (contextRequest.ApplicationPath != "/")
            {
                if (localurl.StartsWith(contextRequest.ApplicationPath))
                {
                    localurl = localurl.Substring(contextRequest.ApplicationPath.Length);
                }
            }
            HttpWebRequest endpointrequest =
            (HttpWebRequest)WebRequest.Create(endpointurl + localurl);
            
            endpointrequest.Method = contextRequest.HttpMethod;
           
            endpointrequest.Headers.Set("User-Host-Address", contextRequest.UserHostAddress);

            foreach (var hdr in contextRequest.Headers.Keys) {
                var hdrnme = (string)hdr;
                var hdrlval = contextRequest.Headers.Get((string)hdr);
                if ("User-Agent,Host,Connection".Split(",".ToCharArray()).Contains(hdrnme))
                {
                   continue;
                }
                else if (hdrnme == "Accept")
                {
                    endpointrequest.Accept = hdrlval;
                    continue;
                }
                else
                {
                    try
                    {
                        endpointrequest.Headers.Set(hdrnme, hdrlval);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }

            }

            if (endpointrequest.Method.ToUpper().Equals("POST"))
            {
                var memStream = new System.IO.MemoryStream();
               
                using (Stream cntxtreqststrm = contextRequest.InputStream)
                {
                    cntxtreqststrm.Position = 0;
                    byte[] tempBuffer = new byte[cntxtreqststrm.Length];
                    var tempbufl = 0;
                    while ((tempbufl = cntxtreqststrm.Read(tempBuffer, 0, tempBuffer.Length)) > 0)
                    {
                        memStream.Write(tempBuffer, 0, tempbufl);
                    }
                    cntxtreqststrm.Close();
                }

                endpointrequest.ContentLength = memStream.Length;

                using (Stream requestStream = endpointrequest.GetRequestStream())
                {
                    memStream.Position = 0;
                    byte[] tempBuffer = new byte[memStream.Length];
                    var tempbufl = 0;
                    while ((tempbufl = memStream.Read(tempBuffer, 0, tempBuffer.Length)) > 0)
                    {
                        requestStream.Write(tempBuffer, 0, tempbufl);
                    }
                    memStream.Close();
                }
            }

            HttpWebResponse endpointresponse;
            var contextResponse = context.Response;
            var hdrlines = new List<string>();
            try
            {
               
                endpointresponse = (HttpWebResponse)endpointrequest.GetResponse();
                contextResponse.StatusCode = (int)endpointresponse.StatusCode;
                var transferencoding = "";
                foreach (var hdr in endpointresponse.Headers.AllKeys)
                {
                    var hdrval = endpointresponse.Headers.Get(hdr);
                    if ("Transfer-Encoding".Split(",".ToCharArray()).Contains(hdr))
                    {
                        transferencoding = hdrval; continue;
                    }                    
                    if (hdr == "Content-Type")
                    {
                        contextResponse.ContentType = hdrval;
                        continue;
                    }
                    else if (hdr == "Range")
                    {
                        hdrval = hdrval + "";
                    }
                    hdrlines.Add(hdr + ":" + hdrval);
                    contextResponse.Headers.Set(hdr, hdrval);
                }
                if(transferencoding=="chunked")
                {
                    //contextResponse.Headers.Set("Transfer-Encoding", transferencoding);
                }
                hdrlines.Clear();
                Stream receiveStream = endpointresponse.GetResponseStream();
                
                byte[] buff = new byte[65535];
                int bytes = 0;
                long totalRead = 0;
                int lastBytesl = 0;
                byte[] binbytes = null;
                var strmout = contextResponse.OutputStream;
                while (receiveStream.CanRead)
                {
                    try
                    {
                        if ((bytes = receiveStream.Read(buff, 0, buff.Length)) > 0)
                        {

                            if (binbytes == null || lastBytesl != bytes)
                            {
                                if (binbytes != null)
                                {
                                    binbytes = null;
                                }
                                binbytes = new byte[bytes];
                            }
                            System.Array.Copy(buff, binbytes, (lastBytesl = bytes));
                            strmout.Write(binbytes,0,bytes);
                            totalRead += bytes;
                        }
                        else {
                            strmout.Flush();
                            break;
                        }
                    }
                    catch (Exception exc)
                    {
                        totalRead = 0;
                    }
                }

                //contextResponse.Flush();
                //close streams
                endpointresponse.Close();
                try
                {
                    contextResponse.Flush();
                    contextResponse.End();
                }
                catch (Exception)
                {
                }
            }
            catch (System.Net.WebException we)
            {
                contextResponse.StatusCode = 404;
                contextResponse.StatusDescription = "Not Found";
                contextResponse.Write("<h2>Page not found</h2><span>"+ localurl+"</span><span>"+ contextRequest.ApplicationPath+"</span>");
                contextResponse.End();
                return;
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