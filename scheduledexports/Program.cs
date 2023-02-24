using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace scheduledexports
{
    public class Program
    {
        public static void Main(string[] args)
        {
            
            Lnksnk.Core.Net.Endpoints.InitiateMain(args);

            if (!Lnksnk.Core.Net.Endpoints.CommandMainService())
            {
                Lnksnk.Core.Net.Endpoints.RegisterInvokingHostServiceEvents(() => {
                    Task.Run(() =>
                    {
                        while(true)
                        {
                            Thread.Sleep(10 * 1024);
                            Lnksnk.Environment.Environment.CallGC();

                        }
                    });
                    //Sessions.SESSIONS().Start().Wait();
                    /*bcore.Core.Data.Dbms.DBMS().RegisterDbConnection("dbtest", "SqlServer", "Server=LAPTOP-8TTIOF76; User ID=PTOOLS;Password=PTOOLS");
                    bcore.Core.Scheduling.Schedules.SCHEDULES().RegisterSchedule("TEST");
                    bcore.Core.Scheduling.Schedules.SCHEDULES()["TEST"].AddActionDbQuery("export", "dbtest", "/", "testfile<timestamp>.csv","SELECT GETDATE() AS THEDATE");
                    bcore.Core.Scheduling.Schedules.SCHEDULES()["TEST"].Start();*/
                }, () => {
                    //Sessions.SESSIONS().Stop().Wait();
                });

                var hostbldr = Lnksnk.Core.Net.Endpoints.CreateHostBuilder(args);
                hostbldr.Build().Run();
            }
        }
    }
}
