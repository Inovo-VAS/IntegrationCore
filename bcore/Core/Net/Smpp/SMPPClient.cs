using Lnksnk.Core.Net.Smpp.Client;
using Lnksnk.Core.Net.Smpp.Lib;
using Lnksnk.Core.Net.Smpp.Lib.Protocol;
using Bcoring.ES6.Expressions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lnksnk.Core.Net.Smpp
{
    public interface IClient {
        public void MessageSent(SmppMessage smppMessage);
        public void MessageDelivered(SmppMessage smppMessage);
        public void MessageReceived(SmppMessage smppMessage);
    }

    public class SmppMessage {
        public string SmppClientName;
        public string SourceAddress;
        public string DestinationAddress;
        public string Message;
        public string MessageSentRef;
        public string Status;
        public bool NotifyDelivery = true;
    }

    public class SMPPClient
    {
        private BlockingCollection<SmppMessage> sendMessagingQueue = new BlockingCollection<SmppMessage>();
        private BlockingCollection<SmppMessage> receivedMessagingQueue = new BlockingCollection<SmppMessage>();

        private SmppClient client = null;
        private IClient iclient = null;

        private string alias = "";
        private string smscnr = "";
        private string accuname = "";
        private string accpw = "";
        private int port = 0;
        private string smpphost = "";
        private string systype = "";
        private string defaultsystype = "";
        private string name = "";
        private int autoconnectdelay = 3000;
        private int keepalivemilsecinterval = 15000;
        private NumberingPlanIndicator addrNpi = NumberingPlanIndicator.ISDN;
        private TypeOfNumber addrTon = TypeOfNumber.International;
        private bool propschanged = false;

        public SMPPClient(string alias) {
            this.alias = alias;
        }

        public void ApplySettings(string accuname, string accpw, int port, string smpphost, string smscnr, string systype, string defaultsystype, IClient iclient, string name="", int autoconnectdelay = 3000, int keepalivemilsecinterval = 15000, NumberingPlanIndicator addrNpi = NumberingPlanIndicator.ISDN, TypeOfNumber addrTon = TypeOfNumber.International)
        {
            /*client.Properties.SystemID = accuname;
            client.Properties.Password = accpw;
            client.Properties.Port = port; //IP port to use
            client.Properties.Host = smpphost; //SMSC host name or IP Address
            client.Properties.SystemType = systype;
            client.Properties.DefaultServiceType = defaultsystype;
            client.Properties.AddressNpi = addrNpi;
            client.Properties.AddressTon = addrTon;
            client.Properties.SourceAddress = this.smsnr = smscnr;
            client.Properties.InterfaceVersion = InterfaceVersion.v34;
            client.Properties.DefaultEncoding = DataCoding.SMSCDefault;
            client.Properties.DefaultServiceType = ServiceType.DEFAULT;
            */
            this.iclient = iclient;
            this.accuname = accuname;
            this.accpw = accpw;
            this.port = port;
            this.smpphost = smpphost;
            this.smscnr = smscnr;
            this.systype = systype;
            this.defaultsystype = defaultsystype;
        }

        internal void Disconnect()
        {
            if (this.Connected)
            {
                if (this.client != null)
                {
                    this.connectstate = -1;
                    //this.client.Shutdown();
                    Task.WaitAll(
                    Task.Run(async ()=>
                    {
                        if (this.dequeueSendMessageEnabled)
                        {
                            while (this.dequeueSendMessageEnabled)
                            {
                                await Task.Delay(10);
                            }
                        }
                    }), Task.Run(async () =>
                    {
                        if (this.dequeueReceivedMessageEnabled)
                        {
                            while (this.dequeueReceivedMessageEnabled)
                            {
                                await Task.Delay(10);
                            }
                        }
                    }));
                    this.client.Dispose();
                    this.client = null;
                }
                this.connectstate = -1;
                Console.WriteLine(this.alias + ":Disconnected");
            }
        }

        public void Connect() {
            if (!this.Connected)
            {
                this.client = ConnectToSMSC(this.client, this, this.accuname, this.accpw, this.port, this.smpphost, this.smscnr, this.systype, this.defaultsystype, this.alias);
                if (this.Connected) {
                    if (!this.dequeueSendMessageEnabled) {
                        this.dequeueSendMessageEnabled = true;
                        DequeueSendMessages();
                    }
                    if (!this.dequeueReceivedMessageEnabled) {
                        this.dequeueReceivedMessageEnabled = true;
                        DequeueRecievedMessages();
                    }
                    Console.WriteLine(this.client.Name + ":Connected");
                }
            }
        }

        public static SmppClient ConnectToSMSC(SmppClient smppclient, SMPPClient client, string accuname, string accpw, int port, string smpphost, string smscnr, string systype, string defaultsystype, string name = "", int autoconnectdelay = 3000, int keepalivemilsecinterval = 15000, NumberingPlanIndicator addrNpi = NumberingPlanIndicator.ISDN, TypeOfNumber addrTon = TypeOfNumber.International)
        {
            if (smppclient == null)
            {
                smppclient = new SmppClient();
                smppclient.ConnectionStateChanged += client.ConnectionStateChanged;
                smppclient.MessageDelivered += client.MessageDelivered;
                smppclient.MessageReceived += client.MessageReceived;
                smppclient.MessageSent += client.MessageSent;
                smppclient.StateChanged += client.StateChanged;
                smppclient.Name = name;
            }
            try
            {
                smppclient.Shutdown();
                SmppConnectionProperties properties = smppclient.Properties;
                properties.SystemID = accuname;
                properties.Password = accpw;
                properties.Port = port; //IP port to use
                properties.Host = smpphost; //SMSC host name or IP Address
                properties.SystemType = systype;// ServiceType.DEFAULT;
                properties.DefaultServiceType = defaultsystype;// ServiceType.DEFAULT;
                properties.AddressNpi = addrNpi;
                properties.AddressTon = addrTon;
                properties.UseSeparateConnections = false;
                properties.SourceAddress = smscnr;
                properties.DefaultEncoding = DataCoding.SMSCDefault;
                properties.InterfaceVersion = InterfaceVersion.v34;
                properties.UseSeparateConnections = true;

                smppclient.AutoReconnectDelay = autoconnectdelay;

                smppclient.KeepAliveInterval = keepalivemilsecinterval;


                //Start smpp client
                if (smppclient.ConnectionState != SmppConnectionState.Connected) smppclient.ForceConnect(5000);
                return smppclient;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return smppclient;
            }
        }

        private bool dequeueSendMessageEnabled = false;
        private bool dequeueReceivedMessageEnabled = false;
        private async Task DequeueSendMessages() {
            await Task.Run(()=> {
                SmppMessage smppmsg = null;
                while (this.Connected)
                {
                    if (this.sendMessagingQueue.TryTake(out smppmsg, 100)){
                        this.SendMessage(smppmsg);
                    }
                }
                foreach (var smsg in this.sendMessagingQueue.GetConsumingEnumerable())
                {
                    smsg.Status="DISABLED";
                    this.SendMessage(smppmsg);
                }
                this.dequeueSendMessageEnabled = false;
            });
        }

        private async Task DequeueRecievedMessages()
        {
            await Task.Run(() => {
                SmppMessage smppmsg = null;
                while (this.Connected)
                {
                    if(this.receivedMessagingQueue.TryTake(out smppmsg, 100)) { 
                        if (this.iclient != null) {
                            this.iclient.MessageReceived(smppmsg);
                        }
                    }
                }
                foreach (var smsg in this.receivedMessagingQueue.GetConsumingEnumerable())
                {
                    if (this.iclient != null)
                    {
                        this.iclient.MessageReceived(smppmsg);
                    }
                }
                this.dequeueReceivedMessageEnabled = false;
            });
        }

        internal class SMSTextMessage : Lnksnk.Core.Net.Smpp.Client.TextMessage
        {
            protected override SubmitSm CreateSubmitSm(SmppEncodingService smppEncodingService)
            {
                var sm = base.CreateSubmitSm(smppEncodingService);
                sm.SourceAddress.Ton = Lnksnk.Core.Net.Smpp.Lib.TypeOfNumber.International;
                sm.SourceAddress.Npi = Lnksnk.Core.Net.Smpp.Lib.NumberingPlanIndicator.ISDN;
                return sm;
            }
        }

        public void EnqQueueNextMessage(string senderaddress, string message, bool notifydelivery = true, params string[] recipientnr) {

            if (recipientnr != null && recipientnr.Length > 0)
            {
                foreach (var rcpnt in recipientnr)
                {
                    var smppmsg = new SmppMessage {  DestinationAddress = rcpnt, SourceAddress = senderaddress, Message = message, SmppClientName = name, NotifyDelivery = notifydelivery };
                    if (this.Connected)
                    {   
                        while (!this.sendMessagingQueue.TryAdd(smppmsg, 100)) {
                            if (!this.Connected) {
                                if (this.iclient != null)
                                {
                                    smppmsg.Status = "DISABLED";
                                    this.iclient.MessageSent(smppmsg);
                                }
                                break;
                            }
                        }
                    }
                    else {
                        if (this.iclient != null) {
                            smppmsg.Status = "DISABLED"; 
                            this.iclient.MessageSent(smppmsg);
                        }
                    }
                }
            }
        }

        public void SendMessage(SmppMessage smppMessage) {
            this.SendMessage(smppMessage.DestinationAddress, smppMessage.SourceAddress,smppMessage.Message, smppMessage.MessageSentRef, smppMessage.NotifyDelivery);
        }

        public void SendMessage(string recipientnr, string senderaddress, string message,string userreference="", bool notifydelivery = true)
        {
            TextMessage msg = new TextMessage();
            if (userreference != "")
            {
                msg.SubmitUserMessageReference = true;
                msg.UserMessageReference = userreference;
            }
            msg.DestinationAddress = recipientnr; //Receipient number
            msg.SourceAddress = this.smscnr;//  senderaddress == null ? this.smscnr : senderaddress.Equals("") ? this.smscnr : senderaddress; //Originating number
            msg.Text = message;
            msg.RegisterDeliveryNotification = true;
            this.client.SendMessage(msg, 1000);
        }

        private bool started = false;

        private int connectstate = -1;

        public Boolean Connected { get => this.connectstate == (int)SmppConnectionState.Connected; }

        private void StateChanged(object sender, StateChangedEventArgs e)
        {
            if (e.Started)
            {
                this.started = true;
            }
        }

        private void MessageSent(object sender, MessageEventArgs e)
        {
            if (e.ShortMessage != null)
            {
                TextMessage msg = (TextMessage)e.ShortMessage;
                if (this.iclient != null)
                {
                    var smppmsg = new SmppMessage() { SmppClientName= this.client.Name, DestinationAddress = msg.DestinationAddress, SourceAddress = msg.SourceAddress, Message = msg.Text,MessageSentRef=msg.UserMessageReference };
                    this.iclient.MessageSent(smppmsg);
                }
            }
        }

        private void MessageReceived(object sender, MessageEventArgs e)
        {
            if (e.ShortMessage != null)
            {
                TextMessage msg = e.ShortMessage as TextMessage;
                if (this.iclient != null)
                {
                    var smppmsg = new SmppMessage() { SmppClientName = this.client.Name, DestinationAddress = msg.DestinationAddress, SourceAddress = msg.SourceAddress, Message = msg.Text,MessageSentRef=msg.UserMessageReference };
                    if (this.dequeueReceivedMessageEnabled) {
                        this.receivedMessagingQueue.TryAdd(smppmsg, 100);
                    } else {
                        this.iclient.MessageReceived(smppmsg);
                    }
                } 
            }
        }

        private void MessageDelivered(object sender, MessageEventArgs e)
        {
            if (e.ShortMessage != null)
            {
                TextMessage msg = e.ShortMessage as TextMessage;
                if (this.iclient != null)
                {
                    var smppmsg = new SmppMessage() { SmppClientName = this.client.Name, DestinationAddress = msg.DestinationAddress, SourceAddress = msg.SourceAddress, Message = "",Status = (msg.Text.IndexOf("stat:DELIVRD")>-1?"DELIVERED":"UNDELIVERED") };
                    this.iclient.MessageDelivered(smppmsg);
                }
            }
        }

        private void ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            switch (e.CurrentState)
            {
                case SmppConnectionState.Connected:
                    this.connectstate = (int)SmppConnectionState.Connected;
                    break;
                case SmppConnectionState.Connecting:
                    this.connectstate = (int)SmppConnectionState.Connecting;
                    break;
                case SmppConnectionState.Closed:
                    this.connectstate = (int)SmppConnectionState.Closed;
                    break;
            }
        }
    }
}
