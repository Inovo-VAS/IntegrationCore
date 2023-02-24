using System;

namespace Bcoring.ES6.Core.Interop
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public sealed class RequireNewKeywordAttribute : Attribute
    {
    }
}
