using System;
using Bcoring.ES6.Core;
using Bcoring.ES6.Core.Interop;

namespace Bcoring.ES6.BaseLibrary
{
#if !(PORTABLE || NETCORE)
    [Serializable]
#endif
    public sealed class EvalError : Error
    {
        [DoNotEnumerate]
        public EvalError()
        {

        }

        [DoNotEnumerate]
        public EvalError(Arguments args)
            : base(args[0].ToString())
        {

        }

        [DoNotEnumerate]
        public EvalError(string message)
            : base(message)
        {

        }
    }
}
