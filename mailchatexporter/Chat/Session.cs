using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace mailchatexporter.Chat
{
    public class Session
    {
        private System.Xml.XmlReader xmlreader = null;
        private long sessionid = 0;

        public long SessionID => this.sessionid;

        public int Login => this.login;

        public Session(long sessionid,int login, System.IO.TextReader xmlinput) {
            this.sessionid = sessionid;
            this.login = login;
            this.xmlreader = System.Xml.XmlReader.Create(xmlinput);
        }

        private List<ChatEntry> chatEntries = new List<ChatEntry>();

        public List<ChatEntry> ChatEntries => this.chatEntries;
        
        private DateTime sessionStartTime;

        public DateTime StartTime => this.sessionStartTime;

        private int login = 0;

        private string contact = "";
        public string Contact => this.contact;

        public string Agent => this.agent;

        private string agent = "";
        private string chat = "";

        private string currentNodeName = "";
        private string chatstamp = "";
        private string chatsource = "";
        private string attachement = "";
        private bool upload = false;

        public void Prepsession() {
            while (this.xmlreader.Read()) {
                if (this.xmlreader.Depth == 0)
                {
                    if (this.xmlreader.Name.Equals("CHATSESSION"))
                    {
                        if (this.xmlreader.NodeType == System.Xml.XmlNodeType.Element)
                        {
                            sessionStartTime = DateTime.Parse(this.xmlreader.GetAttribute(0));
                        }
                        else if (this.xmlreader.NodeType == System.Xml.XmlNodeType.EndElement)
                        {
                            break;
                        }
                    }
                }
                else if (this.xmlreader.Depth == 1)
                {
                    if (this.xmlreader.NodeType == System.Xml.XmlNodeType.Element)
                    {
                        this.currentNodeName = this.xmlreader.Name;
                        if (this.currentNodeName.Equals("CHAT")|| this.currentNodeName.Equals("FILE"))
                        {
                            if (this.currentNodeName.Equals("FILE")) {
                                this.upload = this.xmlreader.GetAttribute("upload").Equals("end") && this.xmlreader.GetAttribute("result").Equals("0");
                            }
                            this.chatsource = this.xmlreader.GetAttribute("source");
                            this.chatstamp = this.xmlreader.GetAttribute("timestamp");
                        }
                    }
                    else if (this.xmlreader.NodeType == System.Xml.XmlNodeType.EndElement)
                    {
                        this.currentNodeName = this.xmlreader.Name;
                        if (this.currentNodeName.Equals("AGENT"))
                        {
                        }
                        else if (this.currentNodeName.Equals("CONTACT"))
                        {
                        }
                        else if (this.currentNodeName.Equals("CHAT"))
                        {
                            this.chatEntries.Add(new ChatEntry(this, this.chatsource.Equals("Login") ? this.agent : "", this.chatsource.Equals("Contact") ? this.contact : "", this.chat,"", this.chatstamp));
                        }
                        else if (this.currentNodeName.Equals("FILE")) {
                            if (this.upload)
                            {
                                this.chatEntries.Add(new ChatEntry(this, this.chatsource.Equals("Login") ? this.agent : "", this.chatsource.Equals("Contact") ? this.contact : "", "", this.attachement, this.chatstamp));
                            }
                        }
                    }
                }
                else if (this.xmlreader.Depth == 2) {
                    if (this.xmlreader.NodeType == System.Xml.XmlNodeType.Text) {
                        if (this.currentNodeName.Equals("AGENT"))
                        {
                            this.agent = this.xmlreader.Value;
                        }
                        else if (this.currentNodeName.Equals("CONTACT"))
                        {
                            this.contact = this.xmlreader.Value;
                        }
                        else if (this.currentNodeName.Equals("CHAT"))
                        {
                            this.chat = this.xmlreader.Value;
                        }
                        else if (this.currentNodeName.Equals("FILE"))
                        {
                            this.attachement = this.xmlreader.Value;
                        }
                    } 
                }
            }
        }
    }
}
