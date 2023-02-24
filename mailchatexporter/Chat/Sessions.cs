using Lnksnk.Core.Data;
using Lnksnk.Core.Net;
using Lnksnk.Environment;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;

namespace mailchatexporter.Chat
{
    public class Sessions
    {
        private static Sessions sessions = new Sessions();
        public static Sessions SESSIONS() {
            return sessions;
        }

        private static string smtphost = "smtp.medihelp.co.za";
        private static string smtpaccount = "presence@medihelp.co.za";
        private static string smtppword = "unRT4@4ZneX47";
        private static string defaulttoaddress="ejoubert@inovo.co.za";

        private static string prodorqaphrase = "";

        private static string defaultDownloadRetrieverURL = "https://webchat.medihelp.co.za/Retriever/";

        private static string ticketrefurl="http://172.24.8.159:8080/MedihelpERPService/MedihelpService?action=GetChatTicketNumber&username=XLAYER&password=XLAYER&chatid=";
        private static List<String[]> transcodings = new List<string[]>();
        public async Task Start() {
            if (System.IO.File.Exists(Lnksnk.Environment.Environment.ENV().Root + "mailchatexporter.conf")) {
                var strmrd = new System.IO.StreamReader(Lnksnk.Environment.Environment.ENV().Root + "mailchatexporter.conf");
                var s = "";
                while (!strmrd.EndOfStream) {
                    if ((s = strmrd.ReadLine()) != null && s != "")
                    {
                        if (s.StartsWith("MEDIHELP-SMTPHOST="))
                        {
                            smtphost = s.Substring("MEDIHELP-SMTPHOST=".Length).Trim();
                        }
                        else if (s.StartsWith("MEDIHELP-SMTPACCOUNT="))
                        {
                            smtpaccount = s.Substring("MEDIHELP-SMTPACCOUNT=".Length).Trim();
                        }
                        else if (s.StartsWith("MEDIHELP-SMTPPASSWORD="))
                        {
                            smtppword = s.Substring("MEDIHELP-SMTPPASSWORD=".Length).Trim();
                        }
                        else if (s.StartsWith("MEDIHELP-TOADDRESS="))
                        {
                            defaulttoaddress = s.Substring("MEDIHELP-TOADDRESS=".Length).Trim();
                        }
                        else if (s.StartsWith("TICKETREF-URL="))
                        {
                            ticketrefurl = s.Substring("TICKETREF-URL=".Length).Trim();
                        }
                        else if (s.StartsWith("PRODQAPHRASE="))
                        {
                            prodorqaphrase = s.Substring("PRODQAPHRASE=".Length).Trim();
                        }
                        else if (s.StartsWith("DOWNLOADREF-URL="))
                        {
                            defaultDownloadRetrieverURL = s.Substring("DOWNLOADREF-URL=".Length).Trim();
                        }
                        else if (s.StartsWith("TRANSCODINGS=")) {
                            var str = s.Substring("TRANSCODINGS=".Length).Trim();
                            foreach(var si in str.Split("|",StringSplitOptions.RemoveEmptyEntries))
                            {
                                if (si.IndexOf("=") > 0) {
                                    var sis = si.Split("=", StringSplitOptions.RemoveEmptyEntries);
                                    if (sis.Length == 2) {
                                        transcodings.Add(sis);
                                    }
                                }
                            }
                        }
                    }
                }
                strmrd.Close();
                strmrd = null;
            }
            await Task.Run(()=> {
                new Thread(() => { this.ProcessSessions(); }).Start();
                new Thread(() => { this.MailSessions(); }).Start();
            });
        }

        public async Task Stop() { }

        public void downLoadAttachment(string remotehostpath, string remoteroot, string sessionid, string attachementfile) {
            var fpath = attachementfile;
            var url = remotehostpath+"?retreivethis="+remoteroot + sessionid + "\\" + fpath;
            var localfilepath = Lnksnk.Environment.Environment.ENV().Root+"downloads/" + sessionid+"/";
            if (!System.IO.Directory.Exists(localfilepath))
            {
                System.IO.Directory.CreateDirectory(localfilepath);
            }
            if (!System.IO.File.Exists(localfilepath + fpath))
            {
                var f = File.Create(localfilepath + fpath, 81920);
                Lnksnk.Core.Net.Endpoints.ExecuteRemoteRequest(url, (buffer, index, count) =>
                {
                    f.Write(buffer, index, count);
                },null,null,null,null);
                f.Flush();
                f.Close();
            }
        }

        private void MailSessions() {
            DBConnection dbcn = null;
            while (true)
            {
                Task.Run(async () => { 
                if (dbcn == null)
                {
                    dbcn = Lnksnk.Core.Data.Dbms.DBMS().DbConnection("mailchatexporter");
                }
                if (dbcn != null) {
                    DataExecutor dbexecrqstargvsession = null;
                    Dictionary<string, object> dbexecrqstargvsessionparams = null;
                    var recssessionstoarchive = dbcn.Query("SELECT TOP 1000 SESSIONID FROM PTOOLS.CHATSESSION WHERE NOT EXISTS(SELECT TOP 1 1 FROM PTOOLS.CHATSESSIONARCHIVED WHERE CHATSESSIONARCHIVED.SESSIONID=CHATSESSION.SESSIONID)");
                    if (recssessionstoarchive != null) {
                        foreach (var rec in recssessionstoarchive) {

                            if ((dbexecrqstargvsessionparams == null ? dbexecrqstargvsessionparams = new Dictionary<string, object>() : dbexecrqstargvsessionparams).ContainsKey("SESSIONID"))
                            {
                                dbexecrqstargvsessionparams["SESSIONID"] = rec.Data[0].ToString();
                            }
                            else {
                                dbexecrqstargvsessionparams.Add("SESSIONID", rec.Data[0].ToString());
                            }

                            (dbexecrqstargvsession == null ? dbexecrqstargvsession = dbcn.Executor() : dbexecrqstargvsession).Execute("INSERT INTO PTOOLS.CHATSESSIONARCHIVED (SESSIONID) SELECT TOP 1 SESSIONID FROM PTOOLS.CHATSESSION WHERE CHATSESSION.SESSIONID=@@SESSIONID@@ AND NOT EXISTS(SELECT TOP 1 1 FROM PTOOLS.CHATSESSIONARCHIVED WHERE CHATSESSIONARCHIVED.SESSIONID=CHATSESSION.SESSIONID)", dbexecrqstargvsessionparams);
                        }
                    }
                    if (dbexecrqstargvsessionparams != null) {
                        dbexecrqstargvsessionparams.Clear();
                    }
                    if (dbexecrqstargvsession != null) {
                        dbexecrqstargvsession.Dispose();
                    }
                    recssessionstoarchive.Dispose();
                }

                    await Task.Delay(10 * 1000);
                }).Wait();
            }
        }

        string medihelpiconafr = "";//  @"<svg width=""150"" height=""60""><path fill-rule=""evenodd"" clip-rule=""evenodd"" fill=""#8CC541"" d=""M32.738 13.462c0 1.188-.374 2.119-1.466 3.369-2.527 3.119-6.703 7.952-6.703 7.952-.812.904-1.687 1.215-2.714 1.215-1.719 0-3.272-1.278-3.272-3.056v-5.676c0-1.465 1.119-2.682 2.615-2.682h4.71c0 1.281-.779 2.091-2.274 2.091h-1.623c-.875 0-1.406.685-1.406 1.559l-.029 4.457c0 .469.28.877.623 1.125.219.157.47.22.779.22.468 0 .842-.251 1.219-.654.592-.749 4.023-4.835 6.518-7.829.499-.656.872-1.277.872-2.09 0-.841-.373-1.5-.872-2.151a2838.263 2838.263 0 01-6.518-7.765c-.377-.408-.751-.717-1.219-.717-.31 0-.561.095-.779.25a1.393 1.393 0 00-.623 1.151l.029 4.429c0 .905.531 1.56 1.406 1.56.062.029 1.557 0 1.623 0 1.495 0 2.274.84 2.274 2.12h-1.184l-3.526-.03c-1.496 0-2.615-1.213-2.615-2.715V3.949c0-1.746 1.554-3.086 3.272-3.086 1.027 0 1.902.373 2.714 1.213 0 0 4.176 4.834 6.703 7.956 1.092 1.279 1.466 2.212 1.466 3.43M2.175 13.462c0 1.188.405 2.119 1.465 3.369 2.527 3.119 6.677 7.952 6.677 7.952.81.904 1.745 1.215 2.71 1.215 1.747 0 3.339-1.278 3.339-3.056v-5.676c0-1.465-1.157-2.682-2.652-2.682H9.005c0 1.281.811 2.091 2.307 2.091h1.593c.872 0 1.403.685 1.403 1.559v4.457a1.327 1.327 0 01-1.373 1.345c-.467 0-.843-.251-1.183-.654-.659-.749-4.058-4.835-6.522-7.829-.562-.656-.932-1.277-.932-2.09 0-.841.37-1.5.932-2.151 2.465-2.965 5.863-7.048 6.522-7.765.34-.408.716-.717 1.183-.717.311 0 .531.095.778.25.375.218.595.654.595 1.151V8.66c0 .905-.531 1.56-1.403 1.56-.032.029-1.529 0-1.593 0-1.496 0-2.307.84-2.307 2.12h1.183l3.525-.03c1.495 0 2.652-1.213 2.652-2.715V3.949c0-1.746-1.592-3.086-3.339-3.086-.965 0-1.9.373-2.71 1.213 0 0-4.149 4.834-6.677 7.956-1.059 1.279-1.464 2.212-1.464 3.43""/><path fill-rule=""evenodd"" clip-rule=""evenodd"" fill=""#1A4A9C"" d=""M69.916 35.824h-6.178v-1.655h3.463c-.063-1.088-.404-1.933-.966-2.526-.594-.623-1.343-.906-2.341-.906h-.156v-2.369h.156c1.839 0 3.277.657 4.399 1.901 1.09 1.279 1.622 3.028 1.622 5.18v.375zm-6.178 4.271c.281.03.593.065.938.065 1.84 0 3.489-.564 5.021-1.688v2.684c-.876.561-1.716.968-2.525 1.216-.843.219-1.781.372-2.904.372-.214 0-.373 0-.529-.058v-2.591zm0-4.271v-1.655h-3.491c.155-1.088.56-1.933 1.217-2.526.561-.594 1.339-.871 2.274-.906v-2.369c-1.841.031-3.368.718-4.519 1.967-1.221 1.342-1.78 3.056-1.78 5.146 0 1.432.247 2.682.841 3.743.564 1.058 1.312 1.933 2.278 2.559.841.526 1.872.839 3.18.904v-2.591c-.904-.187-1.62-.53-2.243-1.12-.813-.78-1.248-1.843-1.309-3.151h3.552zM91.528 22.817c.469 0 .84.156 1.122.468.344.312.532.687.532 1.126 0 .465-.188.874-.532 1.151a1.393 1.393 0 01-1.122.531c-.407 0-.779-.189-1.124-.531-.312-.345-.466-.686-.466-1.151 0-.411.154-.786.466-1.098.345-.305.717-.496 1.124-.496m-1.375 5.831h2.743v13.881h-2.743V28.648zM124.744 35.824h-6.239v-1.655h3.524c-.094-1.088-.404-1.933-.966-2.526-.625-.623-1.404-.906-2.368-.906h-.19v-2.369h.19c1.805 0 3.304.657 4.396 1.901 1.12 1.279 1.653 3.028 1.653 5.18v.375zm-6.239 4.271c.313.03.654.065.999.065 1.777 0 3.461-.564 4.99-1.688v2.684c-.842.561-1.685.968-2.526 1.216a11.09 11.09 0 01-2.901.372c-.187 0-.371 0-.562-.058v-2.591zm0-4.271v-1.655h-3.461c.186-1.088.593-1.933 1.215-2.526.595-.594 1.34-.871 2.246-.906v-2.369c-1.807.031-3.307.718-4.489 1.967-1.187 1.342-1.779 3.056-1.779 5.146 0 1.432.252 2.682.844 3.743.56 1.058 1.309 1.933 2.246 2.559.871.526 1.901.839 3.179.904v-2.591c-.871-.187-1.592-.53-2.212-1.12-.813-.78-1.249-1.843-1.31-3.151h3.521zM85.29 21.477l.03 21.053h-5.862c-.124 0-.248-.033-.405-.033v-2.371c.375.033.842.033 1.404.033h2.089v-8.795a5.378 5.378 0 00-2.558-.654c-.31 0-.654.029-.936.063v-2.404c.185-.031.343-.031.528-.031.906 0 1.903.221 2.965.624v-4.77s-.125-2.745 2.744-2.745v.03zm-6.237 21.02v-2.371c-.499-.063-.872-.095-1.153-.217-.467-.157-.905-.47-1.343-.934-.716-.782-1.058-1.87-1.058-3.276 0-1.529.403-2.744 1.213-3.65.623-.685 1.404-1.089 2.341-1.276v-2.404c-1.747.125-3.243.747-4.364 1.932-1.281 1.343-1.936 2.996-1.936 5.054 0 2.152.624 3.931 1.842 5.209 1.121 1.219 2.619 1.871 4.458 1.933M97.578 24.69l.03-3.182c2.842-.032 2.715 2.712 2.715 2.712v6.237c1.153-1.372 2.557-2.089 4.24-2.089.969 0 1.779.25 2.527.718.748.435 1.277 1.059 1.622 1.874.372.809.53 2.024.53 3.648v7.921h-2.744v-8.61c0-.995-.248-1.805-.749-2.43-.499-.625-1.154-.935-1.968-.935-.59 0-1.185.123-1.712.468-.53.309-1.122.809-1.747 1.56v9.947h-2.745V24.69zM141.524 28.741c1.742.22 3.148.78 4.239 1.748 1.374 1.219 2.061 2.932 2.061 5.147 0 2.058-.659 3.772-1.966 5.113-1.155 1.218-2.592 1.873-4.334 1.998v-2.435c.873-.153 1.65-.589 2.272-1.279.845-.871 1.249-2.057 1.249-3.428 0-.937-.187-1.746-.593-2.403-.372-.716-.903-1.186-1.558-1.524-.375-.188-.813-.314-1.371-.376v-2.561zm-6.301 20.958c2.867 0 2.714-2.71 2.714-2.742v-4.804c1.091.407 2.122.625 3.022.625.159 0 .374 0 .565-.031v-2.435c-.25.065-.565.093-.876.093-.904 0-1.775-.246-2.65-.652v-8.577h1.933c.626 0 1.124.062 1.594.126v-2.561c-.5-.061-.968-.091-1.527-.091h-4.805l.03 21.049zM128.205 21.446l.062.063c2.808.033 2.714 2.712 2.714 2.712V42.53h-2.775V21.446zM45.557 32.421v10.106H42.78v-7.731c0-1.562-.219-2.652-.623-3.275-.405-.594-1.121-.908-2.151-.908-.561 0-1.124.126-1.594.407-.466.252-1.023.718-1.648 1.401v10.106h-2.747V28.619h2.747v1.81c1.403-1.375 2.771-2.091 4.115-2.091 1.777 0 3.15.842 4.085 2.495 1.499-1.684 3.024-2.526 4.646-2.526 1.373 0 2.467.499 3.307 1.5.905.933 1.343 2.458 1.343 4.489v8.232h-2.744v-8.265c0-1.182-.251-2.055-.718-2.648-.467-.625-1.155-.939-2.061-.939-1.123-.001-2.213.597-3.18 1.745""/><g fill=""#1A4A9C"" stroke=""#1A4A9C"" stroke-width="".25"" stroke-miterlimit=""10""><path d=""M74.407 55.309h.586l.079.459c.388-.317.817-.555 1.293-.555.437 0 .792.206.975.626.444-.396.919-.626 1.378-.626.619 0 1.15.38 1.15 1.332v2.512h-.697v-2.306c0-.515-.111-.919-.626-.919-.373 0-.737.222-1.063.5 0 .063.009.135.009.198v2.528h-.696V56.76c0-.523-.112-.928-.627-.928-.374 0-.737.222-1.062.5v2.726h-.698v-3.749zM81.77 57.338c.007.706.444 1.229 1.181 1.229.364 0 .674-.111.936-.253l.213.483c-.348.222-.8.341-1.228.341-1.244 0-1.815-.912-1.815-1.981 0-1.141.627-1.942 1.586-1.942.982 0 1.545.753 1.545 1.949v.175H81.77zm1.696-.524c-.008-.522-.262-1.046-.824-1.046-.539 0-.865.476-.872 1.046h1.696zM85.192 57.227c0-1.204.864-2.013 1.974-2.013.198 0 .404.016.595.071v-1.839h.697v5.429c-.294.159-.777.262-1.253.262-1.117 0-2.013-.658-2.013-1.91zm2.569 1.228v-2.567a1.57 1.57 0 00-.548-.087c-.721 0-1.299.507-1.299 1.411 0 .825.499 1.332 1.308 1.332.174-.002.395-.025.539-.089zM89.845 54.088c0-.246.182-.444.443-.444.27 0 .459.19.459.444s-.197.452-.459.452-.443-.198-.443-.452zm.093 1.221h.698v3.749h-.698v-3.749zM92.617 57.338c.009.706.444 1.229 1.182 1.229.364 0 .674-.111.935-.253l.214.483c-.349.222-.8.341-1.229.341-1.244 0-1.813-.912-1.813-1.981 0-1.141.625-1.942 1.584-1.942.983 0 1.546.753 1.546 1.949v.175h-2.419zm1.697-.524c-.009-.522-.263-1.046-.825-1.046-.539 0-.863.476-.872 1.046h1.697zM96.151 58.281c.239.143.564.285.969.285.419 0 .76-.15.76-.539 0-.761-1.759-.507-1.759-1.751 0-.595.467-1.063 1.315-1.063.372 0 .713.079.982.182v.603a2.302 2.302 0 00-.951-.214c-.348 0-.657.103-.657.46 0 .76 1.76.491 1.76 1.775 0 .769-.651 1.117-1.427 1.117-.524 0-.952-.167-1.213-.333l.221-.522zM100.265 57.338c.009.706.444 1.229 1.182 1.229.365 0 .674-.111.936-.253l.214.483c-.349.222-.8.341-1.229.341-1.243 0-1.813-.912-1.813-1.981 0-1.141.626-1.942 1.585-1.942.982 0 1.546.753 1.546 1.949v.175h-2.421zm1.696-.524c-.009-.522-.262-1.046-.823-1.046-.539 0-.864.476-.873 1.046h1.696zM105.963 58.281c.237.143.563.285.967.285.419 0 .761-.15.761-.539 0-.761-1.76-.507-1.76-1.751 0-.595.468-1.063 1.316-1.063.371 0 .713.079.982.182v.603a2.302 2.302 0 00-.951-.214c-.349 0-.658.103-.658.46 0 .76 1.76.491 1.76 1.775 0 .769-.65 1.117-1.427 1.117a2.31 2.31 0 01-1.212-.333l.222-.522zM109.585 53.446h.697v3.218h.016l1.356-1.355h.823l-1.608 1.601 1.767 2.148h-.832l-1.506-1.863h-.016v1.863h-.697v-5.612zM113.958 57.338c.008.706.445 1.229 1.183 1.229.363 0 .674-.111.935-.253l.215.483c-.35.222-.803.341-1.229.341-1.243 0-1.815-.912-1.815-1.981 0-1.141.626-1.942 1.585-1.942.984 0 1.546.753 1.546 1.949v.175h-2.42zm1.697-.524c-.009-.522-.262-1.046-.825-1.046-.539 0-.864.476-.872 1.046h1.697zM117.572 55.309h.587l.08.459c.387-.317.815-.555 1.291-.555.436 0 .792.206.975.626.443-.396.919-.626 1.379-.626.618 0 1.15.38 1.15 1.332v2.512h-.698v-2.306c0-.515-.11-.919-.626-.919-.373 0-.737.222-1.062.5 0 .063.007.135.007.198v2.528h-.697V56.76c0-.523-.111-.928-.627-.928-.371 0-.736.222-1.061.5v2.726h-.698v-3.749zM126.52 58.654c-.285.269-.649.483-1.117.483-.69 0-1.204-.452-1.204-1.173 0-.499.253-.832.634-1.015.261-.126.563-.174.942-.189l.697-.024v-.198c0-.531-.324-.761-.775-.761-.429 0-.754.135-1.022.293l-.246-.483a2.63 2.63 0 011.355-.373c.849 0 1.388.404 1.388 1.308v1.363c0 .642.022.983.069 1.173h-.603l-.118-.404zm-.658-1.395c-.302.008-.5.056-.658.135-.197.111-.293.301-.293.531 0 .389.254.642.649.642.437 0 .745-.262.911-.428v-.903l-.609.023z""/></g></svg>";
        string medihelpiconen = "";// @"<svg width=""150"" height=""60""><path fill=""#17479E"" d=""M139.895 61.752h-1.408c0-.439.268-.877.711-.877.468 0 .683.447.697.877m.951.719v-.295c0-1.242-.643-2.061-1.625-2.061-1.035 0-1.648.945-1.648 2.084 0 1.07.537 2.084 1.801 2.084.432 0 .914-.127 1.32-.439l-.223-.65c-.314.188-.6.279-.943.279a1.002 1.002 0 01-1.02-1.002h2.338zm-4.694 1.728V61.49c0-1.045-.574-1.375-1.197-1.375-.396 0-.873.203-1.293.6-.17-.371-.5-.6-.951-.6-.422 0-.838.195-1.234.531l-.092-.438h-.775v3.99h.92v-2.816c.238-.211.545-.406.875-.406.443 0 .521.371.521.818v2.404h.92v-2.674c0-.053 0-.109-.008-.143.238-.211.543-.406.875-.406.449 0 .527.354.527.801v2.422h.912zm-7.906-2.447h-1.41c0-.439.27-.877.713-.877.469 0 .683.447.697.877m.951.719v-.295c0-1.242-.643-2.061-1.625-2.061-1.035 0-1.648.945-1.648 2.084 0 1.07.535 2.084 1.803 2.084.428 0 .912-.127 1.318-.439l-.223-.65c-.314.188-.598.279-.943.279a1 1 0 01-1.02-1.002h2.338zm-4.693 1.728v-2.615c0-1.174-.598-1.469-1.158-1.469-.467 0-.865.221-1.203.523v-2.414h-.92v5.975h.92v-2.775c.246-.229.566-.424.881-.424.492 0 .553.424.553.871v2.328h.927zm-4.594-.347l-.23-.666c-.252.16-.514.281-.859.281-.719 0-1.057-.609-1.057-1.291 0-.744.443-1.285 1.064-1.285.375 0 .66.121.912.33l.018-.826c-.207-.145-.545-.279-1.035-.279-1.082 0-1.879.896-1.879 2.109 0 1.121.605 2.059 1.785 2.059.498-.001.99-.212 1.281-.432m-4.31-.809c0-1.375-1.701-1.162-1.701-1.797 0-.234.199-.346.514-.346.391 0 .721.127 1.027.262v-.844a2.75 2.75 0 00-1.059-.203c-.875 0-1.371.49-1.371 1.182 0 1.357 1.67 1.09 1.67 1.805 0 .27-.244.422-.621.422-.359 0-.705-.152-.975-.354l-.297.742c.297.201.742.371 1.342.371.781 0 1.471-.347 1.471-1.24m-6.725 1.037l-.113-.752a1.007 1.007 0 01-.27.043c-.189 0-.246-.115-.246-.412v-4.734h-.918v5.182c0 .496.23.877.834.877a1.34 1.34 0 00.713-.204m-4.07-.851a.919.919 0 01-.652.252c-.385 0-.592-.225-.592-.555 0-.338.223-.523.721-.539l.523-.018v.86zm1.004.97c-.055-.188-.068-.482-.068-1.055v-1.578c0-1.047-.545-1.451-1.443-1.451-.482 0-.934.084-1.418.439l.299.633c.23-.152.537-.297.914-.297.404 0 .713.188.713.676v.102l-.645.025c-.928.033-1.52.482-1.52 1.307 0 .785.521 1.283 1.188 1.283.484 0 .814-.203 1.053-.439l.152.355h.775zm-4.243-.347l-.23-.666c-.254.16-.513.281-.857.281-.721 0-1.058-.609-1.058-1.291 0-.744.443-1.285 1.066-1.285.375 0 .658.121.912.33l.014-.826c-.207-.145-.542-.279-1.033-.279-1.081 0-1.88.896-1.88 2.109 0 1.121.607 2.059 1.788 2.059a2.262 2.262 0 001.278-.432m-4.6-3.643h-.922v3.99h.922v-3.99zm.114-1.232c0-.336-.253-.607-.574-.607-.315 0-.567.279-.567.607 0 .346.252.617.567.617.321 0 .574-.28.574-.617m-3.643 4.412a1.525 1.525 0 01-.412.059c-.707 0-1.136-.539-1.136-1.266 0-.783.467-1.264 1.119-1.264.199 0 .307.023.429.059v2.412zm.92.574v-5.738h-.921v1.926a2.421 2.421 0 00-.459-.035c-1.174 0-2.009.877-2.009 2.117 0 1.35.89 2.051 1.971 2.051.622-.001 1.073-.128 1.418-.321m-5.597-2.211h-1.412c0-.439.27-.877.714-.877.466 0 .682.447.698.877m.95.719v-.295c0-1.242-.644-2.061-1.625-2.061-1.035 0-1.648.945-1.648 2.084 0 1.07.537 2.084 1.802 2.084.428 0 .912-.127 1.317-.439l-.223-.65c-.313.188-.597.279-.941.279a1.001 1.001 0 01-1.021-1.002h2.339zm-4.693 1.728V61.49c0-1.045-.575-1.375-1.195-1.375-.399 0-.876.203-1.296.6-.169-.371-.5-.6-.952-.6-.421 0-.834.195-1.233.531l-.094-.438h-.772v3.99h.919v-2.816c.238-.211.546-.406.875-.406.444 0 .52.371.52.818v2.404h.921v-2.674c0-.053 0-.109-.007-.143.238-.211.544-.406.873-.406.453 0 .529.354.529.801v2.422h.912z""/><path fill-rule=""evenodd"" clip-rule=""evenodd"" fill=""#8CC63F"" d=""M34.148 14.656c0 1.284-.404 2.313-1.578 3.67-2.754 3.379-7.269 8.627-7.269 8.627-.881.955-1.836 1.322-2.938 1.322-1.872 0-3.561-1.395-3.561-3.342v-6.131c0-1.613 1.212-2.935 2.827-2.935h5.102c0 1.394-.845 2.275-2.46 2.275h-1.762c-.954 0-1.505.734-1.505 1.689l-.037 4.846c0 .512.294.953.661 1.209.257.186.514.26.844.26.514 0 .918-.295 1.321-.734.661-.809 4.369-5.25 7.086-8.482.55-.696.953-1.394.953-2.274 0-.919-.403-1.616-.953-2.313-2.717-3.23-6.425-7.671-7.086-8.443-.403-.441-.808-.77-1.321-.77-.33 0-.587.11-.844.256-.367.257-.661.735-.661 1.249l.037 4.808c0 .991.551 1.689 1.505 1.689.073.037 1.688 0 1.762 0 1.615 0 2.46.918 2.46 2.312h-1.285l-3.817-.037c-1.615 0-2.827-1.32-2.827-2.936v-6.13C18.804 2.432 20.492 1 22.364 1c1.102 0 2.057.403 2.938 1.322 0 0 4.515 5.249 7.269 8.626 1.173 1.395 1.577 2.387 1.577 3.708M1 14.656c0 1.284.439 2.313 1.579 3.67 2.753 3.379 7.268 8.627 7.268 8.627.881.955 1.872 1.322 2.938 1.322 1.871 0 3.597-1.395 3.597-3.342v-6.131c0-1.613-1.248-2.935-2.863-2.935H8.415c0 1.394.882 2.275 2.497 2.275h1.725c.954 0 1.542.734 1.542 1.689v4.846c0 .512-.257.953-.661 1.209-.257.186-.514.26-.844.26-.514 0-.918-.295-1.285-.734-.698-.809-4.406-5.25-7.085-8.482-.587-.696-.991-1.394-.991-2.274 0-.919.404-1.616.991-2.313 2.679-3.23 6.387-7.671 7.085-8.443.367-.441.771-.77 1.285-.77.33 0 .587.11.844.256.404.257.661.735.661 1.249v4.808c0 .991-.588 1.689-1.542 1.689-.037.037-1.651 0-1.725 0-1.615 0-2.497.918-2.497 2.312H9.7l3.817-.037c1.615 0 2.863-1.32 2.863-2.936v-6.13C16.381 2.432 14.655 1 12.784 1c-1.065 0-2.057.403-2.938 1.322 0 0-4.515 5.249-7.268 8.626C1.439 12.343 1 13.335 1 14.656""/><path fill-rule=""evenodd"" clip-rule=""evenodd"" fill=""#17479E"" d=""M74.491 38.957v-.404c0-2.35-.587-4.221-1.762-5.617-1.211-1.393-2.79-2.092-4.772-2.092h-.183c-1.982.037-3.636.771-4.884 2.166-1.32 1.432-1.944 3.303-1.944 5.58 0 1.541.293 2.9.918 4.037.623 1.176 1.431 2.094 2.459 2.791.918.586 2.056.918 3.451.99.183.037.365.037.587.037 1.211 0 2.238-.146 3.157-.404.881-.256 1.799-.697 2.716-1.32v-2.9c-1.651 1.211-3.45 1.836-5.433 1.836-.367 0-.697-.039-1.027-.074-.955-.184-1.763-.588-2.424-1.211-.881-.846-1.357-1.982-1.431-3.414h10.572zm-10.498-1.799c.184-1.176.623-2.094 1.321-2.754.624-.623 1.469-.918 2.46-.955h.183c1.064 0 1.909.295 2.533.955s.991 1.578 1.063 2.754H63.993zM96.48 31.137h2.974v15.088H96.48V31.137zm1.468-6.313c.514 0 .918.145 1.249.477.367.367.55.771.55 1.248s-.183.918-.55 1.248a1.593 1.593 0 01-1.249.551c-.439 0-.845-.184-1.211-.551-.331-.367-.516-.771-.516-1.248 0-.439.185-.844.516-1.211.367-.33.772-.514 1.211-.514M133.996 38.957v-.404c0-2.35-.588-4.221-1.799-5.617-1.174-1.393-2.789-2.092-4.771-2.092h-.184c-1.984.037-3.6.771-4.883 2.166-1.285 1.432-1.945 3.303-1.945 5.58 0 1.541.293 2.9.918 4.037.623 1.176 1.432 2.094 2.459 2.791.918.586 2.055.918 3.451.99.184.037.402.037.588.037 1.211 0 2.273-.146 3.156-.404.918-.256 1.834-.697 2.752-1.32v-2.9c-1.688 1.211-3.486 1.836-5.432 1.836-.367 0-.734-.039-1.064-.074a4.912 4.912 0 01-2.424-1.211c-.883-.846-1.357-1.982-1.43-3.414h10.608zm-10.535-1.799c.219-1.176.66-2.094 1.32-2.754.662-.623 1.469-.918 2.461-.955h.184c1.064 0 1.908.295 2.568.955.625.66.955 1.578 1.064 2.754H123.461zM91.194 23.355v-.037c-3.121 0-2.975 2.975-2.975 2.975v5.176c-1.138-.441-2.238-.662-3.229-.662-.183 0-.367 0-.551.037-1.908.111-3.523.809-4.771 2.129-1.396 1.432-2.093 3.232-2.093 5.469 0 2.352.66 4.26 1.982 5.654 1.248 1.322 2.863 2.02 4.882 2.094.146 0 .294.035.44.035h6.351l-.036-22.87zM88.22 43.656h-2.275c-.587 0-1.101 0-1.505-.037-.551-.074-.954-.111-1.248-.221-.514-.184-.991-.514-1.468-1.027-.771-.846-1.138-2.02-1.138-3.563 0-1.65.44-2.973 1.321-3.963.66-.734 1.506-1.176 2.532-1.359.294-.035.661-.072.991-.072.953 0 1.91.219 2.789.697v9.545zM104.52 26.844l.037-3.451c3.082-.037 2.934 2.936 2.934 2.936v6.791c1.25-1.506 2.791-2.275 4.627-2.275 1.027 0 1.91.258 2.715.77.809.479 1.396 1.176 1.764 2.057.404.881.588 2.203.588 3.965v8.592h-2.973v-9.324c0-1.104-.297-1.984-.809-2.645-.551-.697-1.248-1.027-2.129-1.027-.662 0-1.285.145-1.873.514-.586.33-1.211.881-1.91 1.688V46.23h-2.971V26.844zM156.791 33.156c-1.174-1.064-2.715-1.688-4.588-1.908a11.982 11.982 0 00-1.65-.109h-5.215l.039 22.869c3.082 0 2.934-2.938 2.934-2.973v-5.213c1.178.441 2.277.66 3.27.66.184 0 .404 0 .623-.037 1.873-.109 3.451-.844 4.699-2.166 1.432-1.432 2.129-3.303 2.129-5.543-.001-2.384-.733-4.257-2.241-5.58zm-2.127 9.287a4.383 4.383 0 01-2.461 1.396 8.08 8.08 0 01-.953.072c-.992 0-1.945-.256-2.865-.695v-9.289h2.094c.66 0 1.211.037 1.725.109a5.039 5.039 0 011.469.404 3.678 3.678 0 011.689 1.652c.441.734.66 1.615.66 2.605-.001 1.508-.44 2.793-1.358 3.746zM137.74 23.318l.074.074c3.047.035 2.936 2.936 2.936 2.936v19.896h-3.01V23.318zM48.062 35.25v10.977h-3.011v-8.408c0-1.688-.22-2.863-.661-3.523-.44-.662-1.247-.992-2.35-.992-.623 0-1.211.146-1.725.441-.514.256-1.102.773-1.799 1.506v10.977h-2.975V31.102h2.975v1.98c1.542-1.504 3.01-2.275 4.479-2.275 1.909 0 3.414.918 4.441 2.717 1.615-1.836 3.268-2.752 5.03-2.752 1.468 0 2.68.551 3.597 1.615.955 1.063 1.432 2.717 1.432 4.92v8.92h-2.974V37.27c0-1.287-.256-2.24-.771-2.9-.514-.662-1.248-.992-2.239-.992-1.21-.001-2.387.622-3.449 1.872""/></svg>";
        private void ProcessSessions() {
            while (true) {
                Task.Run(async () => {
                    List<String> successStage = new List<string>();
                    try
                    {
                        var dbcn = Lnksnk.Core.Data.Dbms.DBMS().DbConnection("mailchatexporter");
                        successStage.Add("initiated db connection");
                    Lnksnk.Core.Net.Email.Client client = new Lnksnk.Core.Net.Email.Client();
                        successStage.Add("Connecting to mail server:smtphost:" + smtphost + ", smptpaccount:" + smtpaccount);
                        client.ApplySettings(smtphost: smtphost, smptpaccount: smtpaccount, smtppword: smtppword);
                        client.Connect(smtp: true);
                        successStage.Add("Connected to mailserver");
                        if (dbcn != null)
                        {
                            var dbexecsession = dbcn.Executor();
                            var dbexecsessionparams = new Dictionary<string, object>();
                            var dbexecchats = dbcn.Executor();
                            var dbexecchatsparams = new Dictionary<string, object>();
                            successStage.Add("Query Catchs to mail");
                            var recs = dbcn.Query(@"SELECT TOP (1000) [SESSIONID],[CHATDATA],PINET_SESSIONLOG.LOGIN,PINET_SESSIONLOG.INBOUNDCONTACTID,PINET_SESSIONLOG.INETSERVICEID AS CHATSERVICEID,COALESCE((SELECT TOP 1 NAME FROM [PREP].[PINET_SERVICE] WHERE ID=PINET_SESSIONLOG.INETSERVICEID),'') AS CHATSERVICENAME,PINET_SESSIONLOG.INBOUNDCONTACTID,
INBOUNDSERVICEID,COALESCE((SELECT TOP 1 NAME FROM PREP.PCO_INBOUNDSERVICE WHERE PCO_INBOUNDSERVICE.ID=INBOUNDSERVICEID),'') AS INBOUNDSERVICENAME
  FROM [PREP].[PINET_CHATLOG] INNER JOIN PREP.PINET_SESSIONLOG ON  PINET_SESSIONLOG.ID=PINET_CHATLOG.SESSIONID AND NOT EXISTS(SELECT TOP 1 1 FROM PTOOLS.CHATSESSION WHERE CHATSESSION.SESSIONID=PINET_CHATLOG.SESSIONID)");
                            if (recs != null)
                            {
                                foreach (var rec in recs)
                                {
                                    var clientinfo = new Dictionary<string, object>();
                                    var clientinforecs = dbcn.Query("SELECT TOP 1 CLIENTNUMBER,LANGUAGE FROM PTOOLS.CHATURLSESSIONVARIABLES WHERE SESSIONID=" + rec.Data[0].ToString());
                                    if (clientinforecs.MoveNext())
                                    {
                                        clientinforecs.Populate(clientinfo, "CLIENTNUMBER", "LANGUAGE");
                                    }
                                    clientinforecs.Dispose();
                                    clientinforecs = null;

                                    var chatencoded = rec.Data[1].ToString();
                                    foreach (var sis in transcodings) {
                                        chatencoded = chatencoded.Replace(sis[0], sis[1]);
                                    }
                                    var session = new Session(long.Parse(rec.Data[0].ToString()), int.Parse(rec.Data[2].ToString()), new StringReader(chatencoded));
                                    session.Prepsession();
                                    dbexecsessionparams.Clear();
                                    dbexecsessionparams.Add("SESSIONID", session.SessionID);
                                    dbexecsessionparams.Add("LOGIN", session.Login);
                                    dbexecsessionparams.Add("CONTACT", session.Contact);
                                    dbexecsessionparams.Add("SESSIONSTAMP", session.StartTime);
                                    dbexecsession.Execute(@"INSERT INTO [PTOOLS].[CHATSESSION] ([SESSIONID],[LOGIN],[CONTACT],[SESSIONSTAMP]) SELECT @@SESSIONID@@,@@LOGIN@@,@@CONTACT@@,@@SESSIONSTAMP@@", dbexecsessionparams);
                                    var attatchments = new List<string>();
                                    var strbldr = new StringBuilder();
                                    var strwtw = new StringWriter(strbldr);
                                    bool startmessage = false;
                                    var lang = clientinfo.ContainsKey("LANGUAGE") ? clientinfo["LANGUAGE"].ToString() : "EN";
                                    var ticketrefnumber = "";
                                    var tmptracnr = "";
                                    try
                                    {
                                        Lnksnk.Core.Net.Endpoints.ExecuteRemoteRequest(ticketrefurl + session.SessionID.ToString(), (buf, bufi, bufl) =>
                                        {
                                            tmptracnr += UTF8Encoding.UTF8.GetString(buf, bufi, bufl);
                                        });

                                        var tmpticketrefnumber = tmptracnr.Trim();
                                        if (tmpticketrefnumber.IndexOf("reqID=") > 0)
                                        {
                                            tmpticketrefnumber = tmpticketrefnumber.Substring(tmpticketrefnumber.IndexOf("reqID=") + "reqID=".Length);
                                            if (tmpticketrefnumber.IndexOf(",") > 0)
                                            {
                                                if ((tmpticketrefnumber = tmpticketrefnumber.Substring(0, tmpticketrefnumber.IndexOf(",")).Trim()).Equals("null"))
                                                {
                                                    tmpticketrefnumber = "";
                                                }
                                                ticketrefnumber = tmpticketrefnumber;
                                            }
                                        }
                                    }
                                    catch (Exception)
                                    {
                                    }

                                    while (session.ChatEntries.Count > 0)
                                    {
                                        if (!startmessage)
                                        {
                                            startmessage = true;
                                            strwtw.Write(@"<span style=""font-family:Arial,Tahoma;"">");
                                            strwtw.Write(lang.Equals("EN") ? medihelpiconen : lang.Equals("AF") ? medihelpiconafr : medihelpiconen);
                                            strwtw.WriteLine("<b>Medihelp: " + (lang.Equals("EN") ? "WebChat Transcript" : lang.Equals("AF") ? "Webkletstranskripsie" : "WebChat Transcript") + "</b><br/>");
                                            strwtw.WriteLine("<b>" + (lang.Equals("EN") ? "Chat Date" : lang.Equals("AF") ? "Kletsdatum" : "Chat Date") + ": " + session.StartTime.ToString("yyyy-MM-dd") + "</b><br/>");
                                            strwtw.WriteLine("<b>" + (lang.Equals("EN") ? "Chat Reference Number" : lang.Equals("AF") ? "Klets Verwysing nommer" : "Chat Reference Number") + ": " + session.SessionID.ToString() + "</b><br/>");
                                            strwtw.WriteLine("<b>" + (lang.Equals("EN") ? "Ticket Number" : lang.Equals("AF") ? "Ticket nommer" : "Reference Number") + ": " + ticketrefnumber + "</b><br/>");
                                            strwtw.WriteLine("<b>" + (lang.Equals("EN") ? "Client Number" : lang.Equals("AF") ? "Kliënt nommer" : "Client Number") + ": " + (clientinfo.ContainsKey("CLIENTNUMBER") ? clientinfo["CLIENTNUMBER"] : "") + "</b><br/>");
                                            strwtw.WriteLine("<br/>");
                                            strwtw.WriteLine("<table>");

                                        }
                                        var chatentry = session.ChatEntries[0];
                                        session.ChatEntries.RemoveAt(0);
                                        dbexecchatsparams.Clear();
                                        dbexecchatsparams.Add("SESSIONID", session.SessionID);
                                        dbexecchatsparams.Add("LOGIN", session.Login);
                                        dbexecchatsparams.Add("AGENT", chatentry.Agent);
                                        dbexecchatsparams.Add("CONTACT", chatentry.Contact);
                                        dbexecchatsparams.Add("CHAT", chatentry.Chat);
                                        dbexecchatsparams.Add("ENTRYSTAMP", chatentry.ChatStamp);
                                        dbexecchatsparams.Add("ATTACHMENT", chatentry.Attachment);

                                        strwtw.WriteLine("<tr>");
                                        strwtw.WriteLine(@"<th nowrap=""nowrap"" style=""text-align:left"">");
                                        strwtw.Write(chatentry.ChatStamp.ToString("HH:mm:ss") + " " + (!chatentry.Agent.Equals("") ? chatentry.Agent : chatentry.Contact) + ":");
                                        strwtw.Write("</th>");
                                        if (!chatentry.Attachment.Equals(""))
                                        {
                                            attatchments.Add(chatentry.Attachment);
                                            this.downLoadAttachment(defaultDownloadRetrieverURL, @"C:\inetpub\wwwroot\webchat\webapp\Upload\", session.SessionID.ToString(), chatentry.Attachment);

                                            strwtw.WriteLine("<td>");
                                            if (lang.Equals("AF"))
                                            {
                                                strwtw.Write("aanhangsel : " + chatentry.Attachment);
                                            }
                                            else
                                            {
                                                strwtw.Write("attachment : " + chatentry.Attachment);
                                            }

                                            strwtw.Write("</td>");

                                        }
                                        else
                                        {
                                            strwtw.WriteLine("<td>");
                                            strwtw.Write(chatentry.Chat);
                                            strwtw.Write("</td>");
                                        }
                                        strwtw.WriteLine("</tr>");
                                        dbexecchats.Execute(@"INSERT INTO [PTOOLS].[CHATENTRY] ([SESSIONID],[LOGIN],[CONTACT],[ENTRYSTAMP],[CHAT],[AGENT],[ATTACHMENT]) SELECT  @@SESSIONID@@,@@LOGIN@@,@@CONTACT@@,@@ENTRYSTAMP@@,@@CHAT@@,@@AGENT@@,@@ATTACHMENT@@", dbexecchatsparams);
                                    }
                                    if (startmessage)
                                    {
                                        strwtw.WriteLine("</table></span>");
                                        strwtw.Flush();
                                        var mailsubject =(prodorqaphrase!=""?(prodorqaphrase+"-"):"")+"Medihelp:" + (lang.Equals("AF") ? ((clientinfo.ContainsKey("CLIENTNUMBER") ? clientinfo["CLIENTNUMBER"].ToString() : "") + " Webklets verwysing nommer:" + session.SessionID.ToString() + " Ticket nommer:" + ticketrefnumber) : ((clientinfo.ContainsKey("CLIENTNUMBER") ? clientinfo["CLIENTNUMBER"].ToString() : "") + " WebChat number:" + session.SessionID.ToString() + " Ticket number:" + ticketrefnumber));
                                        if (attatchments.Count > 0)
                                        {
                                            client.Send(
                                                "no-reply@medihelp.co.za".Split(";"),
                                                defaulttoaddress.Split(";"),
                                                mailsubject, "html", strbldr.ToString(0, strbldr.Length), Lnksnk.Environment.Environment.ENV().Root + "downloads/" + session.SessionID.ToString() + "/", attatchments.ToArray());
                                        }
                                        else
                                        {
                                            client.Send(
                                                "no-reply@medihelp.co.za".Split(";"),
                                                defaulttoaddress.Split(";"),
                                                 mailsubject, "html", strbldr.ToString(0, strbldr.Length), "", null);
                                        }
                                    }
                                    clientinfo.Clear();
                                    strbldr.Clear();
                                    strbldr = null;
                                    strwtw.Dispose();
                                    strwtw = null;
                                }
                                successStage.Add("Done exporting Catchs to mail");
                                client.Disconnect(smtp: true);
                                successStage.Add("Disconnect Mail Client");
                                recs.Dispose();
                                successStage.Add("Cleanup query");
                            }
                            dbexecsession.Dispose();
                            dbexecchats.Dispose();
                        }
                    }
                    catch (Exception e) {
                        if (successStage.Count > 0) {
                            Console.WriteLine(string.Join("\r\n", successStage.ToArray()));
                        }
                        Console.WriteLine(e.StackTrace);
                    }
                    finally
                    {
                        if (successStage.Count > 0) {
                            successStage.Clear();
                        }
                    }
                    await Task.Delay(10 * 1000);
                }).Wait();
            }
        }
    }
}
