using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lnksnk.Core.Net.Smpp.Client;
using Lnksnk.Core.Net.Smpp.Lib;
using Lnksnk.Core.Net.Smpp.Lib.Protocol;

namespace Lnksnk.Core.Net.Smpp
{
    public class SmppMS : IClient
    {
        private Dictionary<String, SMPPClient> smppclientdictionary = new Dictionary<string, SMPPClient>();

        private static SmppMS smppMS = new SmppMS();

        private string smppMSdbalias = "";
        private string smppMSsqlselectconfigcommand = "";
        private string smppMSsqlsendmessages = "";
        private string smppMSsqlmessagesent = "";
        private string smppMSsqlmessagereceived = "";
        private string smppMSsqlmessagedelivered = "";

        private int checkconfigseconds = 60;

        public void SetSmppDbAlias(string smppMSdbalias, string smppMSsqlselectconfigcommand,string smppMSsqlsendmessages="",string smppMSsqlmessagesent="",string smppMSsqlmessagereceived="",string smppMSsqlmessagedelivered="", int checkconfigseconds =20) {
            this.smppMSdbalias = smppMSdbalias;
            this.smppMSsqlselectconfigcommand = smppMSsqlselectconfigcommand.Replace('\0','@');
            this.smppMSsqlsendmessages = smppMSsqlsendmessages;
            this.smppMSsqlmessagesent = smppMSsqlmessagesent.Replace("##", "@@").Replace('\0', '@');
            this.smppMSsqlmessagereceived= smppMSsqlmessagereceived.Replace("##", "@@").Replace('\0', '@');
            this.smppMSsqlmessagedelivered = smppMSsqlmessagedelivered.Replace("##", "@@").Replace('\0', '@');
            if (checkconfigseconds < 30){
               this.checkconfigseconds = 30;
            }
            else
            {
                this.checkconfigseconds = checkconfigseconds;
            }
            this.ChechSmppConfig();
            this.CheckSendingMessages();
        }

        private Object lcksmppactions = new object();

        private async Task CheckSendingMessages()
        {
            while (true)
            {
                if (this.smppMSsqlsendmessages != ""&&this.smppMSsqlmessagesent!="")
                {
                    var dbexec = Lnksnk.Core.Data.Dbms.DBMS().DbExecutor(this.smppMSdbalias);
                    var recs = Lnksnk.Core.Data.Dbms.DBMS().DbQuery(this.smppMSdbalias, this.smppMSsqlsendmessages);

                    if (recs != null&& dbexec!=null)
                    {
                        lock (lcksmppactions)
                        {   
                            SMPPClient client = null;
                            var lastName = "";
                            var updatesettings = new Dictionary<string, object>();
                            var startedSending = false;
                            foreach (var rec in recs)
                            {
                                if (!startedSending) {
                                    startedSending = true;
                                    //Console.WriteLine("START SENDING");
                                }
                                if (!rec.Data[0].ToString().Equals(""))
                                {
                                    if (!rec.Data[0].ToString().Equals(lastName))
                                    {
                                        //NAME,MESSAGEREFID,SENDER,RECEIVER,MESSAGE,STATUS
                                        lastName = rec.Data[0].ToString();
                                        if (client != null)
                                        {
                                            client = null;
                                        }
                                    }
                                    var updatestatus = "";
                                    if (this.smppclientdictionary.ContainsKey(lastName))
                                    {
                                        client = this.smppclientdictionary[lastName];
                                        if (client.Connected)
                                        {
                                            //client.SendMessage()
                                            client.EnqQueueNextMessage(rec.Data[2].ToString(), rec.Data[4].ToString(), true, rec.Data[3].ToString());
                                            updatestatus = "SENT";
                                        }
                                        else
                                        {
                                            updatestatus = "DISABLED";
                                        }


                                        if (updatesettings.ContainsKey("MESSAGINGNAME"))
                                        {
                                            updatesettings["MESSAGINGNAME"] = lastName;
                                        }
                                        else
                                        {
                                            updatesettings.Add("MESSAGINGNAME", lastName);
                                        }

                                        if (updatesettings.ContainsKey("MESSAGEREFID"))
                                        {
                                            updatesettings["MESSAGEREFID"] = rec.Data[1].ToString();
                                        }
                                        else
                                        {
                                            updatesettings.Add("MESSAGEREFID", rec.Data[1].ToString());
                                        }

                                        if (updatesettings.ContainsKey("RECIPIENT"))
                                        {
                                            updatesettings["RECIPIENT"] = rec.Data[3].ToString();
                                        }
                                        else
                                        {
                                            updatesettings.Add("RECIPIENT", rec.Data[3].ToString());
                                        }

                                        if (updatesettings.ContainsKey("STATUS"))
                                        {
                                            updatesettings["STATUS"] = updatestatus;
                                        }
                                        else
                                        {
                                            updatesettings.Add("STATUS", updatestatus);
                                        }
                                        dbexec.Execute(this.smppMSsqlmessagesent, updatesettings);
                                    }
                                }
                            }
                            if (startedSending)
                            {
                                //Console.WriteLine("DONE SENDING");
                            }
                        }
                    }
                    if (dbexec != null) {
                        dbexec.Dispose();
                        dbexec = null;
                    }
                }
                await Task.Delay(this.lastcheckconfigseconds * 1000);
            }
        }

        private async Task ChechSmppConfig()
        {
            while (true)
            {
                var recs = Lnksnk.Core.Data.Dbms.DBMS().DbQuery(this.smppMSdbalias, this.smppMSsqlselectconfigcommand);

                if (recs != null)
                {
                    Dictionary<string, bool> aliasestoEnable = null;
                    lock (lcksmppactions)
                    {
                        foreach (var rec in recs)
                        {
                            //SMPPUNAME,SMPPPWORD,SMPPHOST,SMPPPORT,SMPPTYPE,SMSCNUMBER
                            string alias = rec.Data[0].ToString();
                            bool enable = rec.Data[7].ToString().Equals("1");
                            (aliasestoEnable == null ? (aliasestoEnable = new Dictionary<string, bool>()) : aliasestoEnable).Add(alias, enable);
                            this.RegisterSmppConnection(alias, rec.Data[1].ToString(), rec.Data[2].ToString(), int.Parse(rec.Data[4].ToString()), rec.Data[3].ToString(), rec.Data[6].ToString(), rec.Data[5].ToString(), rec.Data[5].ToString());
                        }

                        if (aliasestoEnable != null)
                        {
                            if (aliasestoEnable.Count > 0)
                            {
                                foreach (var smppalias in aliasestoEnable)
                                {
                                    if (this.smppclientdictionary.ContainsKey(smppalias.Key))
                                    {
                                        var client = this.smppclientdictionary[smppalias.Key];
                                        if (smppalias.Value)
                                        {
                                            client.Connect();
                                        }
                                        else
                                        {
                                            client.Disconnect();
                                        }
                                    }
                                }
                            }
                            aliasestoEnable.Clear();
                            aliasestoEnable = null;
                        }
                    }
                }

                if (this.lastcheckconfigseconds != this.checkconfigseconds)
                {
                    this.lastcheckconfigseconds = this.checkconfigseconds;
                }
                await Task.Delay(this.lastcheckconfigseconds * 1000);
            }
        }

        private int lastcheckconfigseconds = 0;

        public void RegisterSmppConnection(string alias, string accuname, string accpw, int port, string smpphost, string smscnr, string systype, string defaultsystype, int autoconnectdelay = 3000, int keepalivemilsecinterval = 15000, NumberingPlanIndicator addrNpi = NumberingPlanIndicator.ISDN, TypeOfNumber addrTon = TypeOfNumber.International) {
            lock (smppclientdictionary) {
                SMPPClient client = null;
                if (smppclientdictionary.ContainsKey(alias)) {
                    client = smppclientdictionary[alias];
                } else
                {
                    client = new SMPPClient(alias);
                    smppclientdictionary.Add(alias, client);
                }
                client.ApplySettings(accuname, accpw, port, smpphost, smscnr, systype, defaultsystype, smppMS);
            }            
        }

        public void SendMessage(string alias, string message, params string[] receiver) {
            if (receiver != null && receiver.Length > 0) {
                if (smppclientdictionary.ContainsKey(alias)) {
                    SMPPClient client = null;
                    lock (smppclientdictionary) {
                        client = smppclientdictionary[alias];
                    }
                    Task.Run(() =>
                    {
                        if (receiver != null && receiver.Length > 0) {
                            client.EnqQueueNextMessage("", message, true, recipientnr: receiver);
                        }
                    });
                }
            }
        }

        private Lnksnk.Core.Data.DataExecutor dbdelivrdexetor = null;

        public void MessageDelivered(SmppMessage smppMessage)
        {
            if (this.smppMSsqlmessagedelivered != "")
            {
                if (this.dbdelivrdexetor == null)
                {
                    this.dbdelivrdexetor = Lnksnk.Core.Data.Dbms.DBMS().DbExecutor(this.smppMSdbalias);
                }
                if (dbdelivrdexetor != null)
                {
                    var delvrdsettings = new Dictionary<string, object>();
                    delvrdsettings.Add("MESSAGINGNAME", smppMessage.SmppClientName);
                    delvrdsettings.Add("SENDER", smppMessage.SourceAddress);
                    delvrdsettings.Add("STATUS", smppMessage.Status);
                    dbdelivrdexetor.Execute(this.smppMSsqlmessagedelivered, delvrdsettings);
                }
            }
        }

        private Lnksnk.Core.Data.DataExecutor dbrespnsexetor = null;

        public void MessageReceived(SmppMessage smppMessage)
        {
            if (this.smppclientdictionary.ContainsKey(smppMessage.SmppClientName))
            {
                if (this.smppMSsqlmessagereceived != "")
                {
                    if (this.dbrespnsexetor == null)
                    {
                        this.dbrespnsexetor = Lnksnk.Core.Data.Dbms.DBMS().DbExecutor(this.smppMSdbalias);
                    }
                    if (dbrespnsexetor != null)
                    {
                        var rspnssettings = new Dictionary<string, object>();
                        rspnssettings.Add("MESSAGINGNAME", smppMessage.SmppClientName);
                        rspnssettings.Add("RECEIVER", smppMessage.SourceAddress);
                        rspnssettings.Add("MESSAGE", smppMessage.Message);
                        dbrespnsexetor.Execute(this.smppMSsqlmessagereceived, rspnssettings);
                    }
                }
            }
        }

        public void MessageSent(SmppMessage smppMessage){}

        public static SmppMS SMPP() {
            return smppMS;
        }
    }
}
