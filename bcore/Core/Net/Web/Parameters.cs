using MimeKit;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lnksnk.Core.Net.Web
{
    public class Parameters
    {
        private BaseHandler baseHandler = null;

        public Parameters(BaseHandler baseHandler) {
            this.baseHandler = baseHandler;
            this.standard = new StandardParams(this);
        }

        private StandardParams standard = null;
        public StandardParams Standard => this.standard;

        public class StandardParams
        {
            private Parameters parameters = null;

            public StandardParams(Parameters parameters) {
                this.parameters = parameters;
            }

            public string[] Names {
                get { return this.parameters.baseHandler.StandardParameters; }
            }

            public string[] this[string key] {
                get { return this.parameters.baseHandler.StandardParameter(key); }
            }

            public string String(string key, params string[] sep)
            {
                var vals = this.parameters.baseHandler.StandardParameter(key);
                return vals != null && vals.Length > 0 ? string.Join(sep != null && sep.Length == 1 ? sep[0] : "", vals) : "";

            }

            ~StandardParams()
            {
                if (this.parameters != null) {
                    this.parameters = null;
                }
            }
        }

        ~Parameters() {
            if (this.baseHandler != null) {
                this.baseHandler = null;
            }
            if (this.standard != null) {
                this.standard = null;
            }
        }
    }
}
