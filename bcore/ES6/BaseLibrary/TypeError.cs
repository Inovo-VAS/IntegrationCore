using System;
using Bcoring.ES6.Core;
using Bcoring.ES6.Core.Interop;

namespace Bcoring.ES6.BaseLibrary
{
    [Prototype(typeof(Error))]
#if !(PORTABLE || NETCORE)
    [Serializable]
#endif
    public sealed class TypeError : Error
    {
        [DoNotEnumerate]
        public TypeError(Arguments args)
            : base(args[0].ToString())
        {

        }

        [DoNotEnumerate]
        public TypeError()
        {

        }

        [DoNotEnumerate]
        public TypeError(string message)
            : base(message)
        {
        }
    }
}
