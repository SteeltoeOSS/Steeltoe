using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    public interface IAttributeValue
    {
         T Match<T>(
             Func<string, T> stringFunction,
             Func<bool, T> booleanFunction,
             Func<long, T> longFunction,
             Func<object, T> defaultFunction);
    }
    [Obsolete("Use OpenCensus project packages")]
    public interface IAttributeValue<T> : IAttributeValue
    {
        T Value { get; }
        M Apply<M>(Func<T, M> function);
    }
}
