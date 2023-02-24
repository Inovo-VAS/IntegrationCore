using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.IO;
using Microsoft.AspNetCore.Http;
using Lnksnk.Core.Net;
using Org.BouncyCastle.Crmf;
using Org.BouncyCastle.Utilities.Net;
using Lnksnk.Environment;
using Lnksnk.Core.Scheduling;

namespace Lnksnkpx
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Endpoints.InitiateMain(args);
            if (!Endpoints.CommandMainService()) {

                Lnksnk.Core.Net.Endpoints.RegisterInvokingHostServiceEvents(() => {
                    Task.Run(async () =>
                    {
                        while (true)
                        {
                            await Task.Delay(10 * 1024);
                            Lnksnk.Environment.Environment.CallGC();

                        }
                    });
                }, () => {
                });
                var hostbldr = Endpoints.CreateHostBuilder(args);
                hostbldr.Build().Run();
            }
        }

        
    }
}
