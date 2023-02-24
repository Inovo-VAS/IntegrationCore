using JamaaTech.Smpp.Net.Client;
using JamaaTech.Smpp.Net.Lib;
using JamaaTech.Smpp.Net.Lib.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WinTestSMSApp.Smpp
{
    class Client
    {
        SmppClient client = null;

        private string smsnr = "";
        public Client(string accuname, string accpw, int port, string smpphost, string smscnr, string systype, string defaultsystype, int autoconnectdelay = 3000, int keepalivemilsecinterval = 15000, NumberingPlanIndicator addrNpi = NumberingPlanIndicator.ISDN, TypeOfNumber addrTon = TypeOfNumber.International)
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
            //Resume a lost connection after 30 seconds

            //Send Enquire Link PDU every 15 seconds

            this.client = ConnectToSMSC(this.client, this, accuname, accpw, port, smpphost, smscnr, systype, defaultsystype);

            //Start smpp client

        }

        public void Stop()
        {
            if (this.client != null)
            {
                this.client.Shutdown();
            }
        }

        public static SmppClient ConnectToSMSC(SmppClient smppclient, Client client, string accuname, string accpw, int port, string smpphost, string smscnr, string systype, string defaultsystype, int autoconnectdelay = 3000, int keepalivemilsecinterval = 15000, NumberingPlanIndicator addrNpi = NumberingPlanIndicator.ISDN, TypeOfNumber addrTon = TypeOfNumber.International)
        {
            if (smppclient == null)
            {
                smppclient = new SmppClient();
                smppclient.ConnectionStateChanged += client.ConnectionStateChanged;
                smppclient.MessageDelivered += client.MessageDelivered;
                smppclient.MessageReceived += client.MessageReceived;
                smppclient.MessageSent += client.MessageSent;
                smppclient.StateChanged += client.StateChanged;
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
                properties.SourceAddress = client.smsnr = smscnr;
                properties.DefaultEncoding = DataCoding.SMSCDefault;
                properties.InterfaceVersion = InterfaceVersion.v34;

                smppclient.AutoReconnectDelay = autoconnectdelay;

                smppclient.KeepAliveInterval = keepalivemilsecinterval;


                //Start smpp client
                smppclient.ForceConnect(5000);

                return smppclient;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return smppclient;
            }
        }

        public bool Start()
        {
            if (this.client == null)
            {
                return false;
            }
            else
            {
                var didstart = true;
                this.client.Start();
                System.Threading.Thread.Sleep(10000);
                return didstart;
            }

        }

        internal class SMSTextMessage : TextMessage
        {
            protected override SubmitSm CreateSubmitSm(SmppEncodingService smppEncodingService)
            {
                var sm = base.CreateSubmitSm(smppEncodingService);
                sm.SourceAddress.Ton = JamaaTech.Smpp.Net.Lib.TypeOfNumber.International;
                sm.SourceAddress.Npi = JamaaTech.Smpp.Net.Lib.NumberingPlanIndicator.ISDN;
                return sm;
            }
        }

        public void SendMessage(string recipientnr, string senderaddress, string message, bool notifydelivery = true)
        {
            TextMessage msg = new TextMessage();
            msg.SubmitUserMessageReference = true;
            msg.DestinationAddress = recipientnr; //Receipient number
            msg.SourceAddress = senderaddress==null?this.smsnr:senderaddress.Equals("")?this.smsnr:senderaddress; //Originating number
            msg.Text = message;
            msg.RegisterDeliveryNotification = true;
            this.client.SendMessage(msg, 1000);
        }

        private void MessageEnded(System.IAsyncResult result)
        {

        }

        private void StateChanged(object sender, StateChangedEventArgs e)
        {
            if (e.Started)
            {
                Console.WriteLine("STARTED");
            }
        }

        private void MessageSent(object sender, MessageEventArgs e)
        {
            if (e.ShortMessage != null)
            {
                TextMessage msg = (TextMessage)e.ShortMessage;
                switch (msg.MessageState)
                {
                    case MessageState.EnRoute:
                        Console.WriteLine("EnRoute");
                        break;
                    case MessageState.Delivered:
                        Console.WriteLine("Delivered");
                        break;
                    case MessageState.Expired:
                        Console.WriteLine("Expired");
                        break;
                    case MessageState.Deleted:
                        Console.WriteLine("Deleted");
                        break;
                    case MessageState.Undeliverable:
                        Console.WriteLine("EnRoute");
                        break;
                    case MessageState.Accepted:
                        Console.WriteLine("Accepted");
                        break;
                    case MessageState.Unknown:
                        Console.WriteLine("Unknown");
                        break;
                    case MessageState.Rejected:
                        Console.WriteLine("Rejected");
                        break;
                }
                Console.WriteLine("SourceAddress:"+ (msg.SourceAddress == null ? "" : msg.SourceAddress));
                Console.WriteLine("DestinationAddress:"+ (msg.DestinationAddress == null ? "" : msg.DestinationAddress));
                Console.WriteLine("MSG-SENT:" + (msg.Text == null ? "" : msg.Text));
            }
        }

        private void MessageReceived(object sender, MessageEventArgs e)
        {
            if (e.ShortMessage != null)
            {
                TextMessage msg = e.ShortMessage as TextMessage;
                switch (msg.MessageState)
                {
                    case MessageState.EnRoute:
                        Console.WriteLine("EnRoute");
                        break;
                    case MessageState.Delivered:
                        Console.WriteLine("Delivered");
                        break;
                    case MessageState.Expired:
                        Console.WriteLine("Expired");
                        break;
                    case MessageState.Deleted:
                        Console.WriteLine("Deleted");
                        break;
                    case MessageState.Undeliverable:
                        Console.WriteLine("EnRoute");
                        break;
                    case MessageState.Accepted:
                        Console.WriteLine("Accepted");
                        break;
                    case MessageState.Unknown:
                        Console.WriteLine("Unknown");
                        break;
                    case MessageState.Rejected:
                        Console.WriteLine("Rejected");
                        break;
                }
                Console.WriteLine("Sender:" + msg.SourceAddress);
                Console.WriteLine("MSG-RECEIVED:" + (msg.Text == null ? "" : msg.Text));
            }
        }

        private void MessageDelivered(object sender, MessageEventArgs e)
        {
            if (e.ShortMessage != null)
            {
                TextMessage msg = e.ShortMessage as TextMessage;
                switch (msg.MessageState)
                {
                    case MessageState.EnRoute:
                        Console.WriteLine("EnRoute");
                        break;
                    case MessageState.Delivered:
                        Console.WriteLine("Delivered");
                        break;
                    case MessageState.Expired:
                        Console.WriteLine("Expired");
                        break;
                    case MessageState.Deleted:
                        Console.WriteLine("Deleted");
                        break;
                    case MessageState.Undeliverable:
                        Console.WriteLine("EnRoute");
                        break;
                    case MessageState.Accepted:
                        Console.WriteLine("Accepted");
                        break;
                    case MessageState.Unknown:
                        Console.WriteLine("Unknown");
                        break;
                    case MessageState.Rejected:
                        Console.WriteLine("Rejected");
                        break;
                }
                Console.WriteLine("SourceAddress:"+ (msg.SourceAddress == null ? "" : msg.SourceAddress));
                Console.WriteLine("DestinationAddress:"+ (msg.DestinationAddress == null ? "" : msg.DestinationAddress));
                Console.WriteLine("MSG-DELIVERED:" + (msg.Text == null ? "" : msg.Text));
            }
        }

        private void ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            switch (e.CurrentState)
            {
                case SmppConnectionState.Connected:
                    Console.WriteLine("Connected");
                    break;
                case SmppConnectionState.Connecting:
                    Console.WriteLine("Connecting");
                    break;
                case SmppConnectionState.Closed:
                    Console.WriteLine("Closed");
                    break;
            }
        }
    }
}
