using System;
using Bcoring.ES6.Core;
using Bcoring.ES6.Core.Interop;

namespace Bcoring.ES6.BaseLibrary
{
    [Prototype(typeof(Error))]
#if !(PORTABLE || NETCORE)
    [Serializable]
#endif
    public sealed class URIError : Error
    {
        [DoNotEnumerate]
        public URIError()
        {

        }

        [DoNotEnumerate]
        public URIError(Arguments args)
            : base(args[0].ToString())
        {

        }

        [DoNotEnumerate]
        public URIError(string message)
            : base(message)
        {

        }
    }
}
