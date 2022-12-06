// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;

public class HystrixRequestContext : IDisposable
{
    private static readonly AsyncLocal<HystrixRequestContext> RequestVariables = new();

    public static bool IsCurrentThreadInitialized
    {
        get
        {
            HystrixRequestContext context = RequestVariables.Value;
            return context != null && context.State != null;
        }
    }

    public static HystrixRequestContext ContextForCurrentThread
    {
        get
        {
            if (IsCurrentThreadInitialized)
            {
                return RequestVariables.Value;
            }

            return null;
        }
    }

    internal ConcurrentDictionary<IDisposable, object> State { get; set; }

    private HystrixRequestContext()
    {
    }

    public static void SetContextOnCurrentThread(HystrixRequestContext state)
    {
        RequestVariables.Value = state;
    }

    public static HystrixRequestContext InitializeContext()
    {
        var context = new HystrixRequestContext
        {
            State = new ConcurrentDictionary<IDisposable, object>()
        };

        RequestVariables.Value = context;
        return context;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && State != null)
        {
            foreach (IDisposable v in State.Keys)
            {
                try
                {
                    v.Dispose();

                    State.TryRemove(v, out _);
                }
                catch (Exception)
                {
                    // Intentionally left empty.
                }
            }

            State = null;
        }
    }
}
