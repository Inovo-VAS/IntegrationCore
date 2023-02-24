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
using mailchatexporter.Chat;
using System.Text;

namespace mailchatexporter
{
    public class Program
    {
        public static void Main(string[] args)
        {

            Lnksnk.Core.Net.Endpoints.InitiateMain(args);
            if (!Lnksnk.Core.Net.Endpoints.CommandMainService())
            {
                Lnksnk.Core.Net.Endpoints.RegisterInvokingHostServiceEvents(() => {
                    Sessions.SESSIONS().Start().Wait();
                },() => {
                    Sessions.SESSIONS().Stop().Wait();
                });

                var hostbldr = Lnksnk.Core.Net.Endpoints.CreateHostBuilder(args);
                hostbldr.Build().Run();
            }
        }
    }
}
