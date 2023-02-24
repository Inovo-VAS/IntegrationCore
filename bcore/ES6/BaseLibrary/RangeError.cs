using System;
using Bcoring.ES6.Core;
using Bcoring.ES6.Core.Interop;

namespace Bcoring.ES6.BaseLibrary
{
    [Prototype(typeof(Error))]
#if !(PORTABLE || NETCORE)
    [Serializable]
#endif
    public sealed class RangeError : Error
    {
        [DoNotEnumerate]
        public RangeError()
        {

        }

        [DoNotEnumerate]
        public RangeError(Arguments args)
            : base(args[0].ToString())
        {

        }

        [DoNotEnumerate]
        public RangeError(string message)
            : base(message)
        {

        }
    }
}
