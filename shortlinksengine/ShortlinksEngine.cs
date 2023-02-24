using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shortlinksengine
{
    public class ShortlinksEngine
    {
        public static void RegisterShortlinks(Lnksnk.Core.Data.DataReader reader,string dbalias) {
            Lnksnk.Core.Data.DataExecutor dbrqst = null;
            var extendedparams = (Dictionary<String, Object>)null;
            foreach (var rec in reader)
            {
                //rec["ID"]
                var orgurl = (string)rec["ORIGINALURL"];
                var rqstref = (string)rec["REQUESTREFERENCE"];
                var lnkstate = (string)rec["LINKSTATE"];
                var lnkuname = (string)rec["LINKUNAME"];
                var lnkpw = (string)rec["LINKPW"];
                var lnkmod = (string)rec["LINKMODULE"];
                var lnvendor = (string)rec["LINKVENDOR"];
                var activelink = "";
                var reasoncode = 0;
                var reasondescription = "";
                

                if (lnkstate == "Y") { 
                    if (lnkmod== "PolicyPortal")
                    {
                        if(PolicyPortal.RegisterLink(lnkuname,lnkpw,orgurl,out activelink))
                        {

                        }
                        lnkstate = "1";
                    } else
                    {
                        lnkstate = "1";
                    }
                    
                } else
                {
                    lnkstate = "2";
                }

                if ((extendedparams==null?extendedparams= new Dictionary<String, Object>(): extendedparams).ContainsKey("ACTIVESHORTLINK"))
                {
                    extendedparams["ACTIVESHORTLINK"] = activelink;
                }
                else
                {
                    extendedparams.Add("ACTIVESHORTLINK", activelink);
                }
                if (extendedparams.ContainsKey("NEWLNKSTATE"))
                {
                    extendedparams["NEWLNKSTATE"] = lnkstate;
                }
                else
                {
                    extendedparams.Add("NEWLNKSTATE", lnkstate);
                }

                if (extendedparams.ContainsKey("REASON"))
                {
                    extendedparams["REASON"] = reasoncode;
                }
                else
                {
                    extendedparams.Add("REASON", reasoncode);
                }
                if (extendedparams.ContainsKey("REASONDESCRIPTION"))
                {
                    extendedparams["REASONDESCRIPTION"] = reasondescription;
                }
                else
                {
                    extendedparams.Add("REASONDESCRIPTION", reasondescription);
                }

                (dbrqst==null?dbrqst = Lnksnk.Core.Data.Dbms.DBMS().DbExecutor(dbalias):dbrqst).Execute("EXECUTE SHORTLINK.spUPDATELINKREQUEST @@ID@@,@@ACTIVESHORTLINK@@,@@NEWLNKSTATE@@,@@REASON@@,@@REASONDESCRIPTION@@", rec, extendedparams);
            }


            //if (portalConnector != null)
            //{
            //   portalConnector = null;
            //}
            if (extendedparams!=null)
            {
                extendedparams.Clear();
                extendedparams = null;
            }
            if (dbrqst != null)
            {
                dbrqst.Dispose();
            }
            //Console.WriteLine("DONE REGISTERING SHORTLINKS");
        }
    }
}
