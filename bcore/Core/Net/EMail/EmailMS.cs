using Lnksnk.Core.Net.Email;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lnksnk.Core.Net.EMail
{
    public class EmailMS
    {
        private Dictionary<String, Client> emailclientdictionary = new Dictionary<string, Client>();

        private static EmailMS emailMS = new EmailMS();

        private string emailMSdbalias = "";
        private string emailMSsqlselectconfigcommand = "";
        private string emailMSsqlsendmessages = "";
        private string emailMSsqlmessagesent = "";
        private string emailMSsqlmessagereceived = "";
        private string emailMSsqlmessagedelivered = "";

        private int checkconfigseconds = 30;

        public void SetEmailDbAlias(string emailMSdbalias, string emailMSsqlselectconfigcommand, string emailMSsqlsendmessages = "", string emailMSsqlmessagesent = "", string emailMSsqlmessagereceived = "", string emailMSsqlmessagedelivered = "", int checkconfigseconds = 20)
        {
            this.emailMSdbalias = emailMSdbalias;
            this.emailMSsqlselectconfigcommand = emailMSsqlselectconfigcommand.Replace('\0', '@');
            this.emailMSsqlsendmessages = emailMSsqlsendmessages;
            this.emailMSsqlmessagesent = emailMSsqlmessagesent.Replace("##", "@@").Replace('\0', '@');
            this.emailMSsqlmessagereceived = emailMSsqlmessagereceived.Replace("##", "@@").Replace('\0', '@');
            this.emailMSsqlmessagedelivered = emailMSsqlmessagedelivered.Replace("##", "@@").Replace('\0', '@');
            if (checkconfigseconds < 30)
            {
                this.checkconfigseconds = 30;
            }
            else
            {
                this.checkconfigseconds = checkconfigseconds;
            }
            this.ChechEmailConfig();
            this.CheckSendingMessages();
        }

        private Object lcksmppactions = new object();

        private int lastcheckconfigseconds = 0;

        private async Task ChechEmailConfig()
        {
            while (true)
            {
                var recs = Lnksnk.Core.Data.Dbms.DBMS().DbQuery(this.emailMSdbalias, this.emailMSsqlselectconfigcommand);

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
                            var cnsettings = new Dictionary<string, object>();

                            this.RegisterEmailConnection(alias, cnsettings);// rec.Data[1].ToString(), rec.Data[2].ToString(), int.Parse(rec.Data[4].ToString()), rec.Data[3].ToString(), rec.Data[6].ToString(), rec.Data[5].ToString(), rec.Data[5].ToString());
                        }

                        if (aliasestoEnable != null)
                        {
                            if (aliasestoEnable.Count > 0)
                            {
                                foreach (var smppalias in aliasestoEnable)
                                {
                                    if (this.emailclientdictionary.ContainsKey(smppalias.Key))
                                    {
                                        var client = this.emailclientdictionary[smppalias.Key];
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

        public void RegisterEmailConnection(string alias,params Dictionary<string, object>[] connectionSettings) { 
        }

        private async Task CheckSendingMessages() { 
        }
    }
}
