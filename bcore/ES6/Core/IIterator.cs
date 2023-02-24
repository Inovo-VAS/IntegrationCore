using System;

namespace Bcoring.ES6.Core
{
    public interface IIterator
    {
        IIteratorResult next(Arguments arguments = null);
        IIteratorResult @return();
        IIteratorResult @throw(Arguments arguments = null);
    }
}
