using Lnksnk.Core.Net.Web;
using MailKit.Net.Imap;
using MailKit.Net.Pop3;
using MailKit.Net.Smtp;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lnksnk.Core.Net.Email
{
    public interface IClient
    {
        public void MessageSent(MailMessage emailMessage);
        public void MessageDelivered(MailMessage emailMessage);
        public void MessageReceived(MailMessage emailMessage);
    }

    public class MailMessage
    {
        public string SmppClientName;
        public string SourceAddress;
        public string DestinationAddress;
        public string Message;
        public string MessageSentRef;
        public string Status;
        public bool NotifyDelivery = true;
    }

    public class Client
    {
        ImapClient imapClient = null;
        SmtpClient smtpClient = null;
        Pop3Client popClient = null;

        public bool Connected {
            get => this.isConnected();
        }

        private string smtphost = "";
        private int smtport = 25;
        private string smptpaccount = "";
        private string smtppword = "";

        public void ApplySettings(
              string smtphost="",int smtport=25,string smptpaccount="",string smtppword="")
        {
            this.smtphost = smtphost;
            this.smtport = smtport;
            this.smptpaccount = smptpaccount;
            this.smtppword = smtppword;
        }

            private bool isConnected()
        {
            if (this.imapClient != null)
            {
                return this.imapClient.IsConnected;
            }
            else if (this.smtpClient != null&&this.popClient==null)
            {
                return this.smtpClient.IsConnected;
            }
            else if (this.popClient!=null&&this.smtpClient==null) {
                return this.popClient.IsConnected;
            }
            else if (this.popClient != null && this.smtpClient != null)
            {
                return this.popClient.IsConnected && this.smtpClient.IsConnected;
            }
            return false;
        }

        public bool Connect(bool smtp=false,bool imapi=false,bool pop=false) {

            if (smtp) {
                this.ConnectToSmtp(this.smtphost, this.smtport, this.smptpaccount, this.smtppword);
            }
            return false;
        }

        public void Disconnect(bool smtp = false, bool imapi = false, bool pop = false) {
            if (this.smtpClient != null) {
                this.smtpClient.Disconnect(true);
            }
        }

        private void ConnectToSmtp(string smtphost, int port, string account, string password)
        {
            if (this.smtpClient != null)
            {
                this.smtpClient.Disconnect(true);
            }
            else {
                this.smtpClient = new SmtpClient();
            }
            try
            {
                this.smtpClient.Connect(host: smtphost, port: port);
                this.smtpClient.Authenticate(account, password);
            }
            catch (Exception e) {
                Console.WriteLine("smtpconnect error:" + e.Message);
            }
        }

        public void Send(string[] from, string[] to, string subject,string msgformat,string message,string attachmentsroot, object [] attachments) {
            if (this.smtpClient != null && this.smtpClient.IsConnected && this.smtpClient.IsAuthenticated)
            {
                if (attachmentsroot == null) {
                    attachmentsroot = "";
                }
                var msg = new MimeMessage();
                foreach (var frm in from)
                {
                    msg.From.Add(new MailboxAddress("", frm));
                }

                foreach (var t in to)
                {
                    msg.To.Add(new MailboxAddress("", t));
                }
                msg.Subject = subject;
                if (msgformat == null || (msgformat = msgformat.Trim()).Equals("")) {
                    msgformat = "plain";
                }
                var msgbody= new TextPart(msgformat)
                {
                    Text = message
                };

                Multipart multipart = null;
                if (attachments != null && attachments.Length > 0) {
                   
                    foreach (var att in attachments) {
                        if (att is string) {
                            var attpath = ((string)att).Replace("\\","/");
                            if (!attachmentsroot.Equals("")) {
                                if (attachmentsroot.EndsWith("/"))
                                {
                                    if (attpath.StartsWith("/"))
                                    {
                                        attpath = attachmentsroot + attpath.Substring(1);
                                    }
                                    else {
                                        attpath = attachmentsroot + attpath;
                                    }
                                }
                                else {
                                    if (attpath.StartsWith("/"))
                                    {
                                        attpath = attachmentsroot + attpath;
                                    }
                                    else
                                    {
                                        attpath = attachmentsroot +"/"+ attpath;
                                    }
                                }
                            }
                            if (System.IO.File.Exists(attpath))
                            {
                                var mimetype = Mimetypes.FindExtMimetype(attpath);
                                var attachment = new MimePart(mimetype.Substring(0, mimetype.IndexOf("/")), mimetype.Substring(mimetype.IndexOf("/") + 1))
                                {
                                    Content = new MimeContent(System.IO.File.OpenRead(attpath), ContentEncoding.Default),
                                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                                    ContentTransferEncoding = ContentEncoding.Base64,
                                    FileName = System.IO.Path.GetFileName(attpath)
                                };
                                if (multipart == null) {
                                    multipart = new Multipart("mixed");
                                    multipart.Add(msgbody);
                                }
                                multipart.Add(attachment);
                            }
                        }
                    }
                }
                if (multipart == null)
                {
                    msg.Body = msgbody;
                }
                else {
                    msg.Body = multipart;
                }
                this.smtpClient.Send(msg);
            }
        }
    }
}
