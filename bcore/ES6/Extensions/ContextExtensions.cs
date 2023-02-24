using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bcoring.ES6.Core;

namespace Bcoring.ES6.Extensions
{
    public static class ContextExtensions
    {
        public static void Add(this Context context, string key, object value)
        {
            context.DefineVariable(key).Assign(value);
        }
    }
}
