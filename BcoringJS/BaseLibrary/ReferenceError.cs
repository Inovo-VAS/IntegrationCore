using System;
using Bcoring.ES6.Core;
using Bcoring.ES6.Core.Interop;

namespace Bcoring.ES6.BaseLibrary
{
    [Prototype(typeof(Error))]
#if !(PORTABLE || NETCORE)
    [Serializable]
#endif
    public sealed class ReferenceError : Error
    {
        [DoNotEnumerate]
        public ReferenceError(Arguments args)
            : base(args[0].ToString())
        {

        }

        [DoNotEnumerate]
        public ReferenceError()
        {

        }

        [DoNotEnumerate]
        public ReferenceError(string message)
            : base(message)
        {
        }
    }
}
