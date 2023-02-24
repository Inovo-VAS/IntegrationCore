using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using System.IO;
using System.Net.Http;
using System.Linq;
using Lnksnk.Core.Background;
using System.Net;
using Org.BouncyCastle.Crypto.EC;
using Lnksnk.Core.Net.Web;
using Lnksnk.Core.Data;

namespace Lnksnk.Core.Net
{
    public class Endpoints
    {
        public static Dictionary<string, object> commands = new Dictionary<string, object>();

        public static void Listen(params string[] ports) {
            foreach (var sports in ports)
                if (!sports.Equals(""))
                {
                    Dictionary<int, EndPoint> prts = commands.ContainsKey("ports") ? (Dictionary<int,EndPoint>)commands["ports"] : null;
                    foreach (var sport in sports.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
                    {
                        bool ssl = sport.StartsWith("ssl:");
                        string certfile = "";
                        var sprt = ssl ? sport.Substring("ssl:".Length) : sport;
                        if (ssl && sprt.StartsWith("certfile:")) {
                            sprt = sprt.Substring("certfile:".Length);
                            if (sprt.IndexOf(":") > 0) {
                                certfile = sprt.Substring(0, sprt.IndexOf(":"));
                                sprt = sprt.Substring(sprt.IndexOf(":") + 1);
                            }
                        }
                        if (sprt.IndexOf("-") > 0)
                        {
                            var fromtoports = sprt.Split("-", StringSplitOptions.RemoveEmptyEntries);
                            if (fromtoports.Length == 2)
                            {
                                int porttostart = 0;
                                int.TryParse(fromtoports[0].Trim(), out porttostart);
                                int porttoend = 0;
                                int.TryParse(fromtoports[1].Trim(), out porttoend);
                                while (porttostart > 0 && porttostart <= porttoend)
                                {
                                    if (porttostart < porttoend)
                                    {
                                        
                                        if ((prts == null ? (prts = new Dictionary<int, EndPoint>()) : prts).ContainsKey(porttostart++)) continue;
                                        prts.Add(porttostart - 1,new EndPoint() { 
                                            Port= porttostart - 1,
                                            Ssl=ssl,
                                            Certificatefile=certfile
                                        });;
                                    }
                                    else
                                    {
                                        if ((prts == null ? (prts = new Dictionary<int, EndPoint>()) : prts).ContainsKey(porttostart++)) continue;
                                        prts.Add(porttostart - 1, new EndPoint()
                                        {
                                            Port = porttostart - 1,
                                            Ssl = ssl,
                                            Certificatefile=certfile
                                        });
                                    }
                                }
                            }
                        }
                        else
                        {
                            int porttoadd = 0;
                            int.TryParse(sprt.Trim(), out porttoadd);
                            if (porttoadd > 0)
                            {
                                if ((prts == null ? (prts = new Dictionary<int, EndPoint>()) : prts).ContainsKey(porttoadd)) continue;
                                prts.Add(porttoadd, new EndPoint()
                                {
                                    Port = porttoadd,
                                    Ssl = ssl,
                                    Certificatefile=certfile
                                });
                            }
                        }
                    }
                    if (prts != null && prts.Count > 0)
                    {
                        if (!commands.ContainsKey("ports"))
                        {
                            commands.Add("ports", prts);
                        }
                    }
                }

        }

        public static bool CommandMainService()
        {
            var commandedService = true;
            if (commands.ContainsKey("service"))
            {
                if (commands["service"].Equals("install") || commands["service"].Equals("uninstall") || commands["service"].Equals("start") || commands["service"].Equals("stop") || commands["service"].Equals("restart"))
                {
                    if (commands["service"].Equals("install"))
                    {
                        var proc = new System.Diagnostics.Process();

                        proc.StartInfo.FileName = "sc";
                        proc.StartInfo.Arguments = "create " + commands["appname"].ToString() + " binPath= \"" + commands["appdirectory"].ToString() + commands["appname"].ToString() + ".exe port=3000\"";
                        proc.Start();
                    }
                    else if (commands["service"].Equals("uninstall"))
                    {
                        var proc = new System.Diagnostics.Process();

                        proc.StartInfo.FileName = "sc";
                        proc.StartInfo.Arguments = "stop " + commands["appname"].ToString();
                        proc.Start();
                        proc = null;

                        proc = new System.Diagnostics.Process();
                        proc.StartInfo.Arguments = "stop " + commands["appname"].ToString();
                        proc.StartInfo.FileName = "sc";
                        proc.Start();
                        proc = null;

                        proc = new System.Diagnostics.Process();
                        proc.StartInfo.Arguments = "delete " + commands["appname"].ToString();
                        proc.StartInfo.FileName = "sc";
                        proc.Start();
                        proc = null;
                    }
                    else if (commands["service"].Equals("start"))
                    {
                        var proc = new System.Diagnostics.Process();

                        proc.StartInfo.FileName = "sc";
                        proc.StartInfo.Arguments = "start " + commands["appname"].ToString();
                        proc.Start();
                        proc = null;
                    }
                    else if (commands["service"].Equals("stop"))
                    {
                        var proc = new System.Diagnostics.Process();

                        proc.StartInfo.FileName = "sc";
                        proc.StartInfo.Arguments = "stop " + commands["appname"].ToString();
                        proc.Start();
                        proc = null;
                    }
                }
            } else
            {
                commandedService = false;
            }
            return commandedService;
        }

        public static void InitiateMain(string[] args)
        {
            var appname = System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName;
            if (appname.LastIndexOf(".") > 0)
            {
                appname = appname.Substring(0, appname.LastIndexOf("."));
            }
            commands.Add("appname", appname);
            var cdebase = Lnksnk.Core.Net.Endpoints.ContextPath();
            if (!cdebase.EndsWith("/")) cdebase = cdebase + "/";
            commands.Add("appdirectory", cdebase);
            if (args != null && args.Length > 0)
            {
                var sports = "";
                var prevarg = "";
                foreach (var arg in args)
                {
                    if (arg.StartsWith("port="))
                    {
                        sports = (sports.Equals("") ? "" : (sports + ",")) + arg.Substring("port=".Length).Trim();
                        if (sports.Equals(""))
                        {
                            sports = "3000";
                        }
                    }
                    else
                    {
                        if (prevarg.Equals("service"))
                        {
                            if (arg.Equals("install") || arg.Equals("uninstall") || arg.Equals("start") || arg.Equals("stop") || arg.Equals("restart"))
                            {
                                if (commands.ContainsKey("service"))
                                {
                                    commands["service"] = arg;
                                }
                                else
                                {
                                    commands.Add("service", arg);
                                }
                            }
                        }
                    }
                    prevarg = arg;
                }
            }
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            SetupApp(app);
        }

        public static string ContextPath()
        {

            var root = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName.Replace("\\", "/");// Assembly.GetAssembly(typeof(Endpoint)).CodeBase.Replace("\\","/");
            root = root.Substring(root.StartsWith("file:///") ? "file:///".Length : 0, root.Length - (root.StartsWith("file:///") ? "file:///".Length : 0));
            root = root.Substring(0, root.LastIndexOf("/") + 1);
            return root;// Path.GetFullPath(AppContext.BaseDirectory).Replace("\\","/");
        }

        private static bool ranConfig = false;

        public static void SetupApp(Microsoft.AspNetCore.Builder.IApplicationBuilder app, bool usewebsockets = false)
        {
            app.UseRouting();
            if (usewebsockets)
            {
                app.UseWebSockets();
            }

            if (!ranConfig) {
                LoadConfig();
            }

            app.Use(MiddelWareRequest);
        }

        private static RequestDelegate MiddelWareRequest(RequestDelegate rqstdel)
        {
            return RequestDelegate;
        }

        public async static Task RemoteRequestDelegate(HttpClient client,Stream response=null)
        {
            using (client) {
                
            }  
        }

        public async static Task RequestDelegate(HttpContext context)
        {
            var cancel = context.RequestAborted;
            if (context.WebSockets.IsWebSocketRequest)
            {
                var socket = await context.WebSockets.AcceptWebSocketAsync();
                await Task.Run(() =>
                {
                    Web.Web.WebSocketRequest(socket, cancel);
                });
            }
            else
            {
                await Task.Run(() =>
                {
                    Web.Web.WebRequest(context.Request, context.Response, cancel);
                });
            }
            Environment.Environment.CallGC();
        }

        public static void ExecuteRemoteRequest(string requestpath, params object[] parameters)
        {
            var context = new DefaultHttpContext();
            context.Request.Path = new PathString(requestpath);
            if (parameters != null && parameters.Length > 0)
            {
                var formoptions = context.FormOptions == null ? (context.FormOptions = new Microsoft.AspNetCore.Http.Features.FormOptions()) : context.FormOptions;
                formoptions.BufferBody = true;
            }
            try
            {
                Lnksnk.Core.Net.Endpoints.RequestDelegate(context).Wait();
            }
            catch (Exception)
            { }
            if (context != null)
            {
                context = null;
            }
        }

        public static void ExecuteRequest(string requestpath, params object[] parameters) {
            var context = new DefaultHttpContext();
            context.Request.Path = new PathString(requestpath);
            if (parameters != null && parameters.Length > 0) {
                var formoptions = context.FormOptions == null ? (context.FormOptions = new Microsoft.AspNetCore.Http.Features.FormOptions()) : context.FormOptions;
                formoptions.BufferBody = true;
            }
            try
            {
                Task.WaitAll(Lnksnk.Core.Net.Endpoints.RequestDelegate(context));
            }
            catch (Exception)
            { }
            if (context != null)
            {
                context = null;
            }
        }

        public static void ExecuteRemoteRequest(string remoterequestpath, Action<byte[], int, int> readsContent) {
            ExecuteRemoteRequest(remoterequestpath, readsContent, null, null, null, null);
        }

        public static void ExecuteRemoteRequest(string remoterequestpath,Action<byte[],int,int> readsContent,string[] headers, string[] contentheaders, Object requestContent, HttpMethod method, params object[] parameters) {
            HttpClient httpclient = new HttpClient(new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });

            HttpContent cntnt = null;
            HttpRequestMessage httpRequest = new HttpRequestMessage()
            {
                Method = method == null ? HttpMethod.Get : method,
                RequestUri = new Uri(remoterequestpath),
                Content =(cntnt= RemoteRequestContent(requestContent))
            };

            if (headers != null && headers.Length > 0) {
                
                foreach(var hdr in headers)
                {
                    var hrdi = hdr.Split(":", StringSplitOptions.RemoveEmptyEntries);
                    if (hrdi!=null&&hrdi.Length==2)
                    {

                        if (hrdi[0].Trim() != "" && hrdi[1].Trim() != "")
                        {
                            if (httpRequest.Headers.Contains(hrdi[0].Trim()))
                            {
                                httpRequest.Headers.Remove(hrdi[0].Trim());
                            }
                            httpRequest.Headers.Add(hrdi[0].Trim(), hrdi[1].Trim());

                        }
                    }
                }
            }

            if (contentheaders != null && contentheaders.Length > 0)
            {

                foreach (var hdr in contentheaders)
                {
                    var hrdi = hdr.Split(":", StringSplitOptions.RemoveEmptyEntries);
                    if (hrdi != null && hrdi.Length == 2)
                    {

                        if (cntnt != null)
                        {

                            if (cntnt.Headers.Contains(hrdi[0].Trim()))
                            {
                                cntnt.Headers.Remove(hrdi[0].Trim());
                            }
                            cntnt.Headers.Add(hrdi[0].Trim(), hrdi[1].Trim());

                        }
                    }
                }
            }

            var httpResponse= httpclient.SendAsync(request: httpRequest).Result;
            var respstream = httpResponse.Content.ReadAsStreamAsync().Result;
            var buffer = new byte[81920];
            var bufferl = 0;

            if (readsContent != null)
            {
                while ((bufferl = respstream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    try
                    {
                        readsContent(buffer, 0, bufferl);
                    }
                    catch (Exception) {
                        break;
                    }
                }
            }
            try
            {
                httpRequest.Dispose();
            }
            catch (Exception) { }
            try
            {
                httpResponse.Dispose();
            }
            catch (Exception) { }
            try
            {
                httpclient.Dispose();
            }
            catch (Exception) { }
        }

        private static HttpContent RemoteRequestContent(object requestContent)
        {
             if(requestContent!=null)
            {
                if (requestContent is string) {
                    return new StringContent((string)requestContent);
                 }
            }
            return null;
        }

        public static void LoadConfig()
        {
            if (!ranConfig) ranConfig = true;
            var root = Lnksnk.Environment.Environment.ENV().Root;
            if (!Lnksnk.Environment.Environment.ENV().ContainsTextReaderCall("/countrycode.csv"))
            {
                Lnksnk.Environment.Environment.ENV().RegisterTextReaderCall("/countrycode.csv", () =>
                {
                    return CountryCodes.ContryCodesStream();
                });
            }
            Logging.Log.LOG().Info("ENV Root:" + root);
            var appname = System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName;
            if (appname.LastIndexOf(".") > 0)
            {
                appname = appname.Substring(0, appname.LastIndexOf("."));
            }
            if (System.IO.File.Exists(root + (root.EndsWith("/")?"":"/") + appname + ".conf.js"))
            {
                Logging.Log.LOG().Info("Loading:" + "/" + appname + ".conf.js");
                ExecuteRequest("/" + appname + ".conf.js");
                Logging.Log.LOG().Info("Loaded:" + "/" + appname + ".conf.js");
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .UseSystemd()
            .UseWindowsService()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    Lnksnk.Core.Net.Endpoints.LoadConfig();
                    webBuilder.UseSockets((configureOptions) =>
                    {
                        configureOptions.MaxReadBufferSize = 65536;
                        configureOptions.MaxWriteBufferSize = 65536;
                        configureOptions.NoDelay = false;
                        configureOptions.IOQueueCount = System.Environment.ProcessorCount * 8;
                    });

                    var commands = Lnksnk.Core.Net.Endpoints.commands;

                    if (commands.ContainsKey("ports"))
                    {
                        Dictionary<int, EndPoint> ports = (Dictionary<int, EndPoint>)commands["ports"];
                        webBuilder.UseSockets();
                        webBuilder.UseKestrel((options) =>
                        {
                            var prtkeys = ports.Keys.ToArray();
                            foreach (var prtk in prtkeys)
                            {
                                var endpnt = ports[prtk];
                                if (endpnt.Port > 0)
                                {
                                    if (endpnt.Ssl)
                                    {
                                        if (!endpnt.Certificatefile.Equals(""))
                                        {
                                            options.ListenAnyIP(endpnt.Port, opts =>
                                            {
                                                opts.UseHttps(Lnksnk.Environment.Environment.ENV().Root + endpnt.Certificatefile);
                                            });
                                        }
                                        else
                                        {
                                            options.ListenAnyIP(endpnt.Port, opts =>
                                            {
                                                opts.UseHttps();
                                            });
                                        }
                                    }
                                    else
                                    {
                                        options.ListenAnyIP(endpnt.Port);
                                    }
                                }
                                endpnt = null;
                                ports[prtk] = null;
                                ports.Remove(prtk);
                            }
                            ports.Clear();
                            options.AllowSynchronousIO = true;

                        });
                    }
                    webBuilder.UseStartup<Lnksnk.Core.Net.Endpoints>();
                }).ConfigureServices(ConfigureServices);



        private static void ConfigureServices(HostBuilderContext hostBuilder, IServiceCollection services)
        {
            lock (InvokableServices) {
                while (InvokableServices.Count > 0) {
                    var invksrvice = InvokableServices[0];
                    InvokableServices.RemoveAt(0);
                    services.AddHostedService<Service>(invksrvice);
                }
            }
        }

        public static void RegisterInvokingHostServiceEvents(Action startEvent,Action stopEvent)
        {
            lock (InvokableServices)
            {
                InvokableServices.Add((services) =>
                {
                    return new Service() { StartEvent=startEvent,StopEvent=stopEvent};
                });
            }
        }

        public static void RegisterInvokingHostService(Service service) {
            lock (InvokableServices) {
                InvokableServices.Add((services) =>
                {
                    return service;
                });
            }
        }

        private static List<Func<IServiceProvider, Service>> InvokableServices = new List<Func<IServiceProvider, Service>>();
    }
}
