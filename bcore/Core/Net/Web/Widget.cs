using System;
using System.Collections.Generic;
using System.Text;

namespace Lnksnk.Core.Net.Web
{
    public class Widget : IDisposable
    {
        public String Name => this.GetType().Name;
        public String FullName => this.GetType().FullName;
        public String PathName => "/" + this.FullName.Replace(".", "/");
        public String Path => "/" + this.FullName.Substring(0, this.GetType().FullName.LastIndexOf(".")+1).Replace(".", "/");

        private RequestHandler rqsthndlr = null;
        public RequestHandler Request { get => this.rqsthndlr; }
        private Widget widgetRef = null;

        private ResourcePath pthrsrc = null;
        public ResourcePath Resource { get => this.pthrsrc; }
        private bool disposedValue;

        public Widget (RequestHandler rqsthndlr, ResourcePath pthrsrc){
            this.widgetRef = this;
            this.pthrsrc = pthrsrc;
            this.rqsthndlr = rqsthndlr;
            this.Initializing(this.widgetRef);
        }

        internal void Print(params object[] ss)
        {
            if (this.pthrsrc != null)
            {
                this.pthrsrc.Print(ss: ss);
            }
        }
        internal void Println(params object[] ss)
        {
            if (this.pthrsrc != null)
            {
                this.pthrsrc.Println(ss: ss);
            }
        }

        public virtual void Initializing(Widget widget) { }

        public virtual void ExecuteWidget() {
            if (this.widgetRef != null) {
                lock (this.widgetRef) {
                    this.LoadingWidget(this.widgetRef);
                }
                lock (this.widgetRef)
                {
                    this.ExecutingWidget(this.widgetRef);
                }
                lock (this.widgetRef)
                {
                    this.UnloadingWidget(this.widgetRef);
                }
            }
        }

        public virtual void LoadingWidget(Widget widgetRef) { }

        public virtual void ExecutingWidget(Widget widgetRef) { }
        public virtual void UnloadingWidget(Widget widgetRef) { }

        public virtual void Disposing(Widget widget) { }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (this.widgetRef != null)
                    {
                        lock (this.widgetRef)
                        {
                            this.Disposing(this.widgetRef);
                        }
                        this.widgetRef = null;
                    }
                    if (this.pthrsrc != null) {
                        this.pthrsrc = null;
                    }
                    if (this.rqsthndlr != null) {
                        this.rqsthndlr = null;
                    }
                }
                disposedValue = true;
            }
        }

       ~Widget()
        {
            Dispose(disposing:!this.disposedValue);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
