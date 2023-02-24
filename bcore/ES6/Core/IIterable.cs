using System;
using Bcoring.ES6.Core.Interop;

namespace Bcoring.ES6.Core
{
    public interface IIterable
    {
        IIterator @iterator();
    }
}
