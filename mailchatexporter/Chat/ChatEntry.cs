using System;
using System.Text;

namespace mailchatexporter.Chat
{
    public class ChatEntry
    {
        private Session session = null;
        private string agent = "";
        public string Agent => this.agent;
        private string contact = "";
        public string Contact => this.contact;
        private DateTime chatstamp;
        public DateTime ChatStamp => this.chatstamp;
        private string chat = "";
        public string Chat => this.chat;
        private string attachment = "";
        public string Attachment => this.attachment;

        public ChatEntry(Session session, string agent, string contact, string chat,string attachment, string chatstamp)
        {
            this.session = session;
            this.agent = agent;
            this.contact = contact;
            this.chatstamp = DateTime.Parse(chatstamp);
            this.chat = chat;
            this.attachment = attachment;
        }
    }
}