using System;
using Bcoring.ES6.Core;
using Bcoring.ES6.Core.Interop;

namespace Bcoring.ES6.BaseLibrary
{
    [Prototype(typeof(Error))]
#if !(PORTABLE || NETCORE)
    [Serializable]
#endif
    public sealed class SyntaxError : Error
    {
        [DoNotEnumerate]
        public SyntaxError()
        {

        }

        [DoNotEnumerate]
        public SyntaxError(Arguments args)
            : base(args[0].ToString())
        {

        }

        [DoNotEnumerate]
        public SyntaxError(string message)
            : base(message)
        {

        }
    }
}
