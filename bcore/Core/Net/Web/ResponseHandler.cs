using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Lnksnk.Core.Net.Web
{
    public class ResponseHandler : BaseHandler
    {
        private HttpWebRequest clienthttprequest = null;
        private HttpWebResponse clienthttpresponse = null;
        internal ResponseHandler(Web web, HttpWebRequest clienthttprequest, HttpWebResponse clienthttpresponse) : base(web)
        {
            if ((this.clienthttprequest = clienthttprequest) != null)
            {
                if (clienthttprequest.Connection != null && clienthttprequest.Connection!="")
                {
                    this.userHostAddress = clienthttprequest.Address.Host;
                }
            }
            this.clienthttpresponse = clienthttpresponse;
            this.activeMap.Add("response", this);
        }

        internal void Execute(HttpWebRequest request, HttpWebResponse response)
        {
            this.ExecuteHandler(request, response, doneReading: () =>
            {
                if (this.Web != null)
                {
                    //this.Web.WriteResponseHeader(response);
                }
            }, doneReadingNothing: () =>
            {
                if (this.Web != null)
                {
                    //this.Web.WriteResponseHeader(response);
                }
            });

        }

    }
}
