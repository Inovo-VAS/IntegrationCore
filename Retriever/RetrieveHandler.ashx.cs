using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Retriever
{
    /// <summary>
    /// Summary description for Handler1
    /// </summary>
    public class RetrieveHandler : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            var request = context.Request;
            
            var response = context.Response;

            var userhostaddress = request.UserHostAddress;

            var fpath = "";
            try { fpath = request.QueryString.Get("retreivethis"); }
             catch (Exception) { }
            if (fpath == null || fpath.Equals("")) {
                try { fpath = request.Form.Get("retreivethis"); }
                catch (Exception) { }
            }
            if (fpath == null) {
                fpath = "";
            }
            
            string requestpath = fpath.Equals("")?request.Path: fpath;

            if (requestpath.IndexOf("?") > 0) {
                requestpath = requestpath.Substring(0, requestpath.IndexOf("?"));
            }

            response.ContentType = Mimetypes.FindExtMimetype(requestpath);
            try
            {
                if (!fpath.Equals("") && System.IO.File.Exists(fpath))
                {
                    var f = System.IO.File.OpenRead(fpath);
                    var o = response.OutputStream;
                    var buffer = new byte[81920];
                    var bufferl = 0;
                    var bufferi = 0;
                    var ol = 0;
                    try
                    {
                        while (true)
                        {
                            if ((bufferl = f.Read(buffer, 0, buffer.Length)) == 0)
                            {
                                break;
                            }
                            else
                            {
                                o.Write(buffer, 0, bufferl);
                            }
                        }
                    }
                    catch (Exception) { }
                    f.Close();
                }
            }
            catch (Exception e) {
               
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