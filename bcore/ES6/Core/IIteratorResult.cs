using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bcoring.ES6.Core
{
    public interface IIteratorResult
    {
        JSValue value { get; }
        bool done { get; }
    }
}
