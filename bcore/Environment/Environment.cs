using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.IO;

namespace Lnksnk.Environment
{
    public class Environment
    {
        private string envroot = "";
        private Dictionary<string, Func<TextReader>> mappedTextReaders = new Dictionary<string, Func<TextReader>>();
        private Dictionary<string, EndPoint> envEndpoints = new Dictionary<string, EndPoint>();
        public bool ContainsTextReaderCall(string readercallname) {
            return mappedTextReaders.ContainsKey(readercallname);
        }

        public Func<TextReader> TextReaderCall(string readercallname)
        {
            Func<TextReader> readercall = null;
            lock (mappedTextReaders)
            {
                if (mappedTextReaders.ContainsKey(readercallname))
                {
                    readercall = mappedTextReaders[readercallname];
                }
            }
            return readercall;
        }

        public void RegisterTextReaderCall(string readercallname, Func<TextReader> readercall)
        {
            if (readercall == null) return;
            lock(mappedTextReaders)
            {
                if (mappedTextReaders.ContainsKey(readercallname)) { 
                    if(mappedTextReaders[readercallname]!=readercall)
                    {
                        mappedTextReaders[readercallname] = readercall;
                    }
                }else
                {
                    mappedTextReaders.Add(readercallname, readercall);
                }
            }
        }

        public string Root { get { 
                return envroot==""?Core.Net.Endpoints.ContextPath():envroot;
            } set { 
                this.envroot = value; 
            } 
        }

        private static Environment env = new Environment();

        public static Environment ENV()
        {
            return env;
        }

        private static bool didCalGC = false;
        private static object lckgc = new object();
        public static void CallGC()
        {
            if (didCalGC) return;
            didCalGC = true;
            Task.Run(() =>
            {
                Task.Delay(100).Wait();
                lock (lckgc)
                {
                    System.GC.Collect();
                    didCalGC = false;
                }
            });
        }
    }
}
