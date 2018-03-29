using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace
{
    public interface IAttributeValue
    {
    }
    public interface IAttributeValue<T> : IAttributeValue
    {
        T Value { get; }
        M Apply<M>(Func<T, M> function);
    }
}
