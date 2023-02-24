using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bcoring.ES6.BaseLibrary;

namespace Bcoring.ES6.Core
{
    public interface ICallable
    {
        FunctionKind Kind { get; }

        JSValue Construct(Arguments arguments);

        JSValue Construct(JSValue targetObject, Arguments arguments);

        JSValue Call(JSValue targetObject, Arguments arguments);
    }
}
