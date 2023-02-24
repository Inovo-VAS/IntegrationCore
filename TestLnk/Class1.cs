using Lnksnk.Core.Net.Web;
using System;

namespace TestLnk
{
    public class Test
    {
        public Test() { 
        }

        public void ExecuteTest(ResourcePath rsrc)
        {
            rsrc.Print("HELL THERE FROM", typeof(Test).Name);
        }

        private String testprop = "THIS PROP";
        public String TestProp
        {
            get { return this.testprop; }
            set { this.testprop = value; }
        }
        
        public string TestThis()
        {
            return "testthis";
        }
    }
}
