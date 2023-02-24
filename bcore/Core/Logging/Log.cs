using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Lnksnk.Core.IO;
using Microsoft.Extensions.Logging;

namespace Lnksnk.Core.Logging
{
    class Log
    {
        private bool logginstarted = false;
        private BlockingCollection<char[]> logbufffer = null;
        public Log()
        {
            this.writer = new StreamWriter(Environment.Environment.ENV().Root + "log.log");
            /*this.blockingRW = new BlockingRW(logbufffer = new BlockingCollection<char[]>(100));
            Task.Run(async () => {
                var didWrite = false;
                while (true) {
                    didWrite = false;
                    foreach (var chrs in logbufffer.GetConsumingEnumerable()) {
                        didWrite = true;
                        this.writer.Write(chrs);
                    }
                    if (didWrite) {
                        this.writer.Flush();
                    }
                    await Task.Delay(10);
                }
            });*/
        }

        private BlockingRW blockingRW = null;

        private StreamWriter writer = null;

        private BlockingRW logrw = null;
        
        private static Log log = new Log();

        private bool debugEnabled = false;
        public bool IsDebugEnabled { get { return this.debugEnabled; }  set { this.debugEnabled = value; } }

        private bool errorEnabled = false;
        public bool IsErrorEnabled { get { return this.errorEnabled; } set { this.errorEnabled = value; } }

        public static Log LOG()
        {
            return log;
        }

        internal void DebugFormat(string message,params object[] args)
        {
            var formattedmsg = string.Format(message, args: args);
            this.blockingRW.Writer.Write(message);
        }

        internal void TraceFormat(string message, params object[] args)
        {
            var formattedmsg = string.Format(message, args: args);
        }

        internal void WarnFormat(string message, params object[] args)
        {
            var formattedmsg = string.Format(message, args: args);
        }

        internal void Error(Exception ex) {
             this.ErrorFormat(ex.Message);
        }

        internal void Error(string ex)
        {
            this.ErrorFormat(ex);
        }

        internal void Info(string info)
        {
            this.writer.WriteLine("I:" + info);
            this.writer.Flush();
        }

        internal void ErrorFormat(string message, params object[] args)
        {
            var formattederrormsg = string.Format(message, args: args);
            this.writer.WriteLine("E:" + message);
            this.writer.Flush();
        }

        internal void Warn(string warn)
        {
            this.writer.WriteLine("W:" + warn);
            this.writer.Flush();
        }
    }
}
