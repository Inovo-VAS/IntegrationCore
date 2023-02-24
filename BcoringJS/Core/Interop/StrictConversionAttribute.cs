using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bcoring.ES6.Core.Interop
{
    [AttributeUsage(
        AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Delegate, 
        AllowMultiple = false, 
        Inherited = false)]
    public sealed class StrictConversionAttribute : Attribute
    {
    }
}
