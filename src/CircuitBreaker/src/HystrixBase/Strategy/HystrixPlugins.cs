// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.EventNotifier;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.ExecutionHook;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Metrics;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;
using Steeltoe.Common.Util;
using System;

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy
{
    public static class HystrixPlugins
    {
        private static readonly AtomicReference<HystrixEventNotifier> _notifier = new AtomicReference<HystrixEventNotifier>();
        private static readonly AtomicReference<HystrixConcurrencyStrategy> _concurrencyStrategy = new AtomicReference<HystrixConcurrencyStrategy>();
        private static readonly AtomicReference<HystrixMetricsPublisher> _metricsPublisher = new AtomicReference<HystrixMetricsPublisher>();
        private static readonly AtomicReference<HystrixCommandExecutionHook> _commandExecutionHook = new AtomicReference<HystrixCommandExecutionHook>();
        private static readonly AtomicReference<HystrixOptionsStrategy> _options = new AtomicReference<HystrixOptionsStrategy>();

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
}
