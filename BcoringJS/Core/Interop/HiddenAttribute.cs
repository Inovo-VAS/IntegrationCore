using System;

namespace Bcoring.ES6.Core.Interop
{
    /// <summary>
    /// Член, помеченный данным аттрибутом, не будет доступен из сценария.
    /// </summary>
#if !(PORTABLE || NETCORE)
    [Serializable]
#endif
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
    public sealed class HiddenAttribute : Attribute
    {
    }
}
