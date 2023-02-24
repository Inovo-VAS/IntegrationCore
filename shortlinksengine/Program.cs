using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lnksnk.Core.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace shortlinksengine
{
    public class Program
    {
        public static void Main(string[] args)
        {
            /*new Thread(() =>
            {
                Thread.Sleep(30 * 1024);
                //var prtlconnect = new PolicyPortalConnector();

                var shrtrul = "";
                if (PolicyPortal.RegisterLink("ZA_EPTP@avon.com", "f@9s#5g&6", "https://eptp.avon.co.za/?testref=122334", out shrtrul))
                {
                    Console.WriteLine(shrtrul);
                }
            }).Start();*/

            Endpoints.InitiateMain(args);

            if (!Endpoints.CommandMainService())
            {
                Lnksnk.Core.Net.Endpoints.RegisterInvokingHostServiceEvents(()=> {
                    //Console.WriteLine("Started"); 
                    Task.Run(async () =>
                    {
                        while (true)
                        {
                            await Task.Delay(10 * 1024);
                            Lnksnk.Environment.Environment.CallGC();

                        }
                    });
                    if (Lnksnk.Core.Scheduling.Schedules.SCHEDULES().RegisterSchedule("registerlinks"))
                    {
                        Lnksnk.Core.Scheduling.Schedules.SCHEDULES()["registerlinks"].Seconds = 30;
                        Lnksnk.Core.Scheduling.Schedules.SCHEDULES()["registerlinks"].AddActionDataQuery("registerlinks", "shortlinkengine", "SELECT TOP 1000 SHORTLINKREQUEST.ID,ORIGINALURL,REQUESTREFERENCE,COALESCE(SHORTLINKCONFIG.ENABLE,'E') AS LINKSTATE,COALESCE(SHORTLINKCONFIG.NAME,'') AS LINKNAME,COALESCE(SHORTLINKCONFIG.UNAME,'') AS LINKUNAME,COALESCE(SHORTLINKCONFIG.PASSWORD,'') AS LINKPW,COALESCE(SHORTLINKCONFIG.MODULE,'') AS LINKMODULE,COALESCE(SHORTLINKCONFIG.VENDOR,'') AS LINKVENDOR FROM (SELECT TOP 1000 * FROM SHORTLINK.SHORTLINKREQUEST WHERE SHORTLINKREQUEST.STATUS=0) SHORTLINKREQUEST LEFT JOIN (SELECT ID,ENABLE,NAME,UNAME,PASSWORD,VENDOR,MODULE FROM SHORTLINK.SHORTLINKCONFIG) SHORTLINKCONFIG ON SHORTLINKREQUEST.SHORTLINKDEFID=SHORTLINKCONFIG.ID", (dbreader,dbalias) => {
                            ShortlinksEngine.RegisterShortlinks(dbreader, dbalias);
                        });
                        Lnksnk.Core.Scheduling.Schedules.SCHEDULES()["registerlinks"].Start();
                    }
                    
                }, () => { 
                        //Console.WriteLine("Stopped"); 
                });
                var hostbldr = Endpoints.CreateHostBuilder(args);
                hostbldr.Build().Run();
            }
        }
    }
}
