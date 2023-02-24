using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bcoring.ES6.Core.Interop
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
    public sealed class ToStringTagAttribute : Attribute
    {
        public string Tag { get; }

        public ToStringTagAttribute(string tag)
        {
            Tag = tag;
        }
    }
}
