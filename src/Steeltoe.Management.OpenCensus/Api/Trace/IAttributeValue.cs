using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace
{
    public interface IAttributeValue
    {
         T Match<T>(
             Func<string, T> stringFunction,
             Func<bool, T> booleanFunction,
             Func<long, T> longFunction,
             Func<object, T> defaultFunction);
    }
    public interface IAttributeValue<T> : IAttributeValue
    {
        T Value { get; }
        M Apply<M>(Func<T, M> function);
    }
}
