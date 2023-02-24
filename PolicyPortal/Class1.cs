using System;
using System.Threading.Tasks;

namespace PolicyPortal
{
    public class PolicyPortalConnector
    {
        private shortnerSoapClient shortnerSoapClient = null;

        public bool RegisterLink(string uname,string pw,string long_url,out string srtndurl) {
            srtndurl = "";
            if (long_url != null && !(long_url = long_url.Trim()).Equals(""))
            {
                if (shortnerSoapClient == null)
                {
                    this.shortnerSoapClient = new shortnerSoapClient(shortnerSoapClient.EndpointConfiguration.shortnerSoap12);
                }
                var shrtrsp = this.shortnerSoapClient.shortenAsync(uname, pw, long_url).Result;
                if (shrtrsp != null && shrtrsp.Body != null)
                {
                    srtndurl = shrtrsp.Body.shortenResult;
                }
                return true;
            }
            return false;
        }

        public static PolicyPortalConnector PortalConnector()
        {
            return new PolicyPortalConnector();
        }
        ~PolicyPortalConnector() {
            if (this.shortnerSoapClient != null) {
                try
                {
                    Task.WaitAll(this.shortnerSoapClient.CloseAsync());
                }
                finally
                {
                    this.shortnerSoapClient = null;
                }
            }
        }
    }
}
