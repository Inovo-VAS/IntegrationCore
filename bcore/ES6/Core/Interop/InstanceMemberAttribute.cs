using System;

namespace Bcoring.ES6.Core.Interop
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class InstanceMemberAttribute : Attribute
    {
    }
}
