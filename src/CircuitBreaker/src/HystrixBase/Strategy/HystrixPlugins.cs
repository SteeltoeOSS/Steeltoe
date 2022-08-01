// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.EventNotifier;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.ExecutionHook;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Metrics;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;
using Steeltoe.Common.Util;

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy;

public static class HystrixPlugins
{
    private static readonly AtomicReference<HystrixEventNotifier> AtomicNotifier = new ();
    private static readonly AtomicReference<HystrixConcurrencyStrategy> AtomicConcurrencyStrategy = new ();
    private static readonly AtomicReference<HystrixMetricsPublisher> AtomicMetricsPublisher = new ();
    private static readonly AtomicReference<HystrixCommandExecutionHook> AtomicCommandExecutionHook = new ();
    private static readonly AtomicReference<HystrixOptionsStrategy> AtomicOptions = new ();

    public static HystrixEventNotifier EventNotifier
    {
        get
        {
            if (AtomicNotifier.Value == null)
            {
                AtomicNotifier.CompareAndSet(null, HystrixEventNotifierDefault.GetInstance());
            }

            return AtomicNotifier.Value;
        }
    }

    public static void RegisterEventNotifier(HystrixEventNotifier impl)
    {
        if (!AtomicNotifier.CompareAndSet(null, impl))
        {
            throw new InvalidOperationException("Another strategy was already registered.");
        }
    }

    public static HystrixConcurrencyStrategy ConcurrencyStrategy
    {
        get
        {
            if (AtomicConcurrencyStrategy.Value == null)
            {
                AtomicConcurrencyStrategy.CompareAndSet(null, HystrixConcurrencyStrategyDefault.GetInstance());
            }

            return AtomicConcurrencyStrategy.Value;
        }
    }

    public static void RegisterConcurrencyStrategy(HystrixConcurrencyStrategy impl)
    {
        if (!AtomicConcurrencyStrategy.CompareAndSet(null, impl))
        {
            throw new InvalidOperationException("Another strategy was already registered.");
        }
    }

    public static HystrixMetricsPublisher MetricsPublisher
    {
        get
        {
            if (AtomicMetricsPublisher.Value == null)
            {
                AtomicMetricsPublisher.CompareAndSet(null, HystrixMetricsPublisherDefault.GetInstance());
            }

            return AtomicMetricsPublisher.Value;
        }
    }

#pragma warning disable S4136 // Method overloads should be grouped together
    public static void RegisterMetricsPublisher(HystrixMetricsPublisher impl)
#pragma warning restore S4136 // Method overloads should be grouped together
    {
        if (!AtomicMetricsPublisher.CompareAndSet(null, impl))
        {
            throw new InvalidOperationException("Another strategy was already registered.");
        }
    }

    public static HystrixCommandExecutionHook CommandExecutionHook
    {
        get
        {
            if (AtomicCommandExecutionHook.Value == null)
            {
                AtomicCommandExecutionHook.CompareAndSet(null, HystrixCommandExecutionHookDefault.GetInstance());
            }

            return AtomicCommandExecutionHook.Value;
        }
    }

    public static void RegisterMetricsPublisher(HystrixCommandExecutionHook impl)
    {
        if (!AtomicCommandExecutionHook.CompareAndSet(null, impl))
        {
            throw new InvalidOperationException("Another strategy was already registered.");
        }
    }

    public static HystrixOptionsStrategy OptionsStrategy
    {
        get
        {
            if (AtomicOptions.Value == null)
            {
                AtomicOptions.CompareAndSet(null, HystrixOptionsStrategyDefault.GetInstance());
            }

            return AtomicOptions.Value;
        }
    }

    public static void RegisterOptionsStrategy(HystrixOptionsStrategy impl)
    {
        if (!AtomicOptions.CompareAndSet(null, impl))
        {
            throw new InvalidOperationException("Another strategy was already registered.");
        }
    }

    public static void Reset()
    {
        AtomicNotifier.Value = null;
        AtomicConcurrencyStrategy.Value = null;
        AtomicMetricsPublisher.Value = null;
        AtomicCommandExecutionHook.Value = null;
        AtomicOptions.Value = null;
        HystrixMetricsPublisherFactory.Reset();
    }
}
