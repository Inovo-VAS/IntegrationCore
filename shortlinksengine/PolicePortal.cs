using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace shortlinksengine
{
    public class PolicyPortal
    {
        public static bool RegisterLink(string uname, string pw, string long_url, out string srtndurl)
        {
            srtndurl = "";

            if (long_url != null && long_url != "")
            {
                var soapRequestStr = @"<s:Envelope xmlns:s=""http://www.w3.org/2003/05/soap-envelope""><s:Body><shorten xmlns=""http://tempuri.org/"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><u>ZA_EPTP@avon.com</u><p>f@9s#5g&amp;6</p><long_url>" + long_url + @"</long_url></shorten></s:Body></s:Envelope>";


                try
                {
                    using (var client = new HttpClient(new HttpClientHandler() { AutomaticDecompression = System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.GZip }) { })
                    {
                        var request = new HttpRequestMessage()
                        {
                            RequestUri = new Uri("https://clkl.ink/api/shortner.asmx"),
                            Method = HttpMethod.Post
                        };

                        request.Content = new StringContent(soapRequestStr, Encoding.UTF8, "text/xml");

                        HttpResponseMessage response = client.SendAsync(request).Result;

                        if (!response.IsSuccessStatusCode)
                        {
                            return false;
                        }

                        Task<Stream> streamTask = response.Content.ReadAsStreamAsync();
                        Stream stream = streamTask.Result;
                        var sr = new StreamReader(stream).ReadToEnd();

                        //var soapResponse = XDocument.Load(sr);
                        //Console.WriteLine(soapResponse);

                        if (sr.IndexOf("<shortenResult>") > 0)
                        {
                            sr = sr.Substring(sr.IndexOf("<shortenResult>") + "<shortenResult>".Length);
                            srtndurl = sr.Substring(0, sr.IndexOf("</shortenResult>"));
                        }
                    }
                }
                catch (AggregateException ex)
                {
                    if (ex.InnerException is TaskCanceledException)
                    {
                        return false;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    return false;
                }

                //srtndurl = long_url;
                return true;
            }

            return false;
        }
    }
}
