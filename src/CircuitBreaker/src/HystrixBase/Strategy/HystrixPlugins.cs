// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.EventNotifier;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.ExecutionHook;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Metrics;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;
using Steeltoe.Common.Util;
using System;

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy;

public static class HystrixPlugins
{
    private static readonly AtomicReference<HystrixEventNotifier> _notifier = new ();
    private static readonly AtomicReference<HystrixConcurrencyStrategy> _concurrencyStrategy = new ();
    private static readonly AtomicReference<HystrixMetricsPublisher> _metricsPublisher = new ();
    private static readonly AtomicReference<HystrixCommandExecutionHook> _commandExecutionHook = new ();
    private static readonly AtomicReference<HystrixOptionsStrategy> _options = new ();

    static HystrixPlugins()
    {
    }

    #region EventNotifier
    public static HystrixEventNotifier EventNotifier
    {
        get
        {
            if (_notifier.Value == null)
            {
                _notifier.CompareAndSet(null, HystrixEventNotifierDefault.GetInstance());
            }

            return _notifier.Value;
        }
    }

    public static void RegisterEventNotifier(HystrixEventNotifier impl)
    {
        if (!_notifier.CompareAndSet(null, impl))
        {
            throw new InvalidOperationException("Another strategy was already registered.");
        }
    }
    #endregion EventNotifier

    #region  ConcurrencyStrategy
    public static HystrixConcurrencyStrategy ConcurrencyStrategy
    {
        get
        {
            if (_concurrencyStrategy.Value == null)
            {
                _concurrencyStrategy.CompareAndSet(null, HystrixConcurrencyStrategyDefault.GetInstance());
            }

            return _concurrencyStrategy.Value;
        }
    }

    public static void RegisterConcurrencyStrategy(HystrixConcurrencyStrategy impl)
    {
        if (!_concurrencyStrategy.CompareAndSet(null, impl))
        {
            throw new InvalidOperationException("Another strategy was already registered.");
        }
    }
    #endregion  ConcurrencyStrategy

    #region  MetricsPublisher
    public static HystrixMetricsPublisher MetricsPublisher
    {
        get
        {
            if (_metricsPublisher.Value == null)
            {
                _metricsPublisher.CompareAndSet(null, HystrixMetricsPublisherDefault.GetInstance());
            }

            return _metricsPublisher.Value;
        }
    }

#pragma warning disable S4136 // Method overloads should be grouped together
    public static void RegisterMetricsPublisher(HystrixMetricsPublisher impl)
#pragma warning restore S4136 // Method overloads should be grouped together
    {
        if (!_metricsPublisher.CompareAndSet(null, impl))
        {
            throw new InvalidOperationException("Another strategy was already registered.");
        }
    }
    #endregion  MetricsPublisher

    #region  CommandExecutionHook
    public static HystrixCommandExecutionHook CommandExecutionHook
    {
        get
        {
            if (_commandExecutionHook.Value == null)
            {
                _commandExecutionHook.CompareAndSet(null, HystrixCommandExecutionHookDefault.GetInstance());
            }

            return _commandExecutionHook.Value;
        }
    }

    public static void RegisterMetricsPublisher(HystrixCommandExecutionHook impl)
    {
        if (!_commandExecutionHook.CompareAndSet(null, impl))
        {
            throw new InvalidOperationException("Another strategy was already registered.");
        }
    }
    #endregion  CommandExecutionHook

    #region  OptionsStrategy
    public static HystrixOptionsStrategy OptionsStrategy
    {
        get
        {
            if (_options.Value == null)
            {
                _options.CompareAndSet(null, HystrixOptionsStrategyDefault.GetInstance());
            }

            return _options.Value;
        }
    }

    public static void RegisterOptionsStrategy(HystrixOptionsStrategy impl)
    {
        if (!_options.CompareAndSet(null, impl))
        {
            throw new InvalidOperationException("Another strategy was already registered.");
        }
    }
    #endregion  OptionsStrategy

    public static void Reset()
    {
        _notifier.Value = null;
        _concurrencyStrategy.Value = null;
        _metricsPublisher.Value = null;
        _commandExecutionHook.Value = null;
        _options.Value = null;
        HystrixMetricsPublisherFactory.Reset();
    }
}
