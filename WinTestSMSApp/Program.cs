using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WinTestSMSApp
{
    class Program
    {
        static void Main(string[] args)
        {
           
            Common.Logging.LogManager.Adapter = new Common.Logging.Simple.DebugLoggerFactoryAdapter();

            var smppclient = new Smpp.Client("9191", "Vod@t60r", 8313, "41.1.224.76", "27820000191", "SMSC", "SMSC");

            System.Threading.Thread.Sleep(1024);
            smppclient.SendMessage("27829689379", "27820000191", "TEST MESSAGE",true);
            //smppclient.SendMessage("0725366646", "27820000191", "TEST MESSAGE", true);
            while (true)
                System.Threading.Thread.Sleep(1024);
        }
    }
}
