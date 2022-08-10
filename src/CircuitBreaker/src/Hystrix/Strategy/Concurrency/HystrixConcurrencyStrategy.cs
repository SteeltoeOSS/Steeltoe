// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;

public class HystrixConcurrencyStrategy
{
    public virtual HystrixTaskScheduler GetTaskScheduler(IHystrixThreadPoolOptions options)
    {
        if (options.MaxQueueSize < 0)
        {
            return new HystrixSyncTaskScheduler(options);
        }

        return new HystrixQueuedTaskScheduler(options);
    }

    public virtual IHystrixRequestVariable<T> GetRequestVariable<T>(T value)
    {
        return new HystrixRequestVariableDefault<T>(value);
    }

    public virtual IHystrixRequestVariable<T> GetRequestVariable<T>(Func<T> valueFactory, Action<T> disposeAction)
    {
        return new HystrixRequestVariableDefault<T>(valueFactory, disposeAction);
    }

    public virtual IHystrixRequestVariable<T> GetRequestVariable<T>(Func<T> valueFactory)
    {
        return new HystrixRequestVariableDefault<T>(valueFactory);
    }
}
