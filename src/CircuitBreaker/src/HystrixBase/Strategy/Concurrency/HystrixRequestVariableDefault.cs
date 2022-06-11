// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using System;

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;

public class HystrixRequestVariableDefault<T> : IHystrixRequestVariable<T>
{
    private readonly Action<T> _disposeAction;
    private readonly Func<T> _valueFactory;

    public HystrixRequestVariableDefault(T value)
    {
        _valueFactory = () => value;
    }

    public HystrixRequestVariableDefault(Func<T> valueFactory, Action<T> disposeAction)
    {
        _valueFactory = valueFactory;
        _disposeAction = disposeAction;
    }

    public HystrixRequestVariableDefault(Func<T> valueFactory)
    {
        _valueFactory = valueFactory;
    }

    internal static void Remove(HystrixRequestContext context, IHystrixRequestVariable<T> v)
    {
        if (context.State.TryRemove(v, out _))
        {
            v.Dispose();
        }
    }

    internal virtual void Remove()
    {
        if (HystrixRequestContext.ContextForCurrentThread != null)
        {
            Remove(HystrixRequestContext.ContextForCurrentThread, this);
        }
    }

    public virtual T Value
    {
        get
        {
            // Checks to make sure HystrixRequestContext.ContextForCurrentThread.State != null
            if (!HystrixRequestContext.IsCurrentThreadInitialized)
            {
                throw new InvalidOperationException("HystrixRequestContext.InitializeContext() must be called at the beginning of each request before RequestVariable functionality can be used.");
            }

            return (T)HystrixRequestContext.ContextForCurrentThread.State.GetOrAddEx(this, k => _valueFactory());
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _disposeAction?.Invoke(Value);
        }
    }
}
