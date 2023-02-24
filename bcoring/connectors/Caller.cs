using Lnksnk.Core.Net.Web;
using Org.BouncyCastle.Bcpg;
using System;
using System.Collections.Generic;
using System.Text;

namespace bcoring.connectors
{
    public class Caller : Lnksnk.Core.Net.Web.Widget
    {
        public Caller(RequestHandler rqsthndlr, ResourcePath pthrsrc) : base(rqsthndlr, pthrsrc)
        {
        }

        public override void ExecutingWidget(Widget widgetRef)
        {
            base.ExecutingWidget(widgetRef);
        }
    }
}
