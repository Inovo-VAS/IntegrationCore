using System;

namespace Bcoring.ES6.Core.Interop
{
    [AttributeUsage(AttributeTargets.Event | AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property)]
    public sealed class JavaScriptNameAttribute : Attribute
    {
        public string Name { get; private set; }

        public JavaScriptNameAttribute(string name)
        {
            Name = name;
        }
    }
}
