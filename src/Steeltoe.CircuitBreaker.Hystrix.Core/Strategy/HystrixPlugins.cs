//
// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.EventNotifier;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.ExecutionHook;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Metrics;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;


namespace Steeltoe.CircuitBreaker.Hystrix.Strategy
{
    public static class HystrixPlugins
    {
        private static readonly AtomicReference<HystrixEventNotifier> notifier = new AtomicReference<HystrixEventNotifier>();
        private static readonly AtomicReference<HystrixConcurrencyStrategy> concurrencyStrategy = new AtomicReference<HystrixConcurrencyStrategy>();
        private static readonly AtomicReference<HystrixMetricsPublisher> metricsPublisher = new AtomicReference<HystrixMetricsPublisher>();
        private static readonly AtomicReference<HystrixCommandExecutionHook> commandExecutionHook = new AtomicReference<HystrixCommandExecutionHook>();
        private static readonly AtomicReference<HystrixOptionsStrategy> options = new AtomicReference<HystrixOptionsStrategy>();

        static HystrixPlugins()
        {

        }


        #region EventNotifier
        public static HystrixEventNotifier EventNotifier
        {
            get
            {
                if (notifier.Value == null)
                {
                    notifier.CompareAndSet(null, HystrixEventNotifierDefault.GetInstance());
                }
                return notifier.Value;
            }
        }

        public static void RegisterEventNotifier(HystrixEventNotifier impl)
        {
            if (!notifier.CompareAndSet(null, impl))
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
                if (concurrencyStrategy.Value == null)
                {
                    concurrencyStrategy.CompareAndSet(null, HystrixConcurrencyStrategyDefault.GetInstance());
                }
                return concurrencyStrategy.Value;
            }
        }

        public static void RegisterConcurrencyStrategy(HystrixConcurrencyStrategy impl)
        {
            if (!concurrencyStrategy.CompareAndSet(null, impl))
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
                if (metricsPublisher.Value == null)
                {
                    metricsPublisher.CompareAndSet(null, HystrixMetricsPublisherDefault.GetInstance());
                }
                return metricsPublisher.Value;
            }
        }

        public static void RegisterMetricsPublisher(HystrixMetricsPublisher impl)
        {
            if (!metricsPublisher.CompareAndSet(null, impl))
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
                if (commandExecutionHook.Value == null)
                {
                    commandExecutionHook.CompareAndSet(null, HystrixCommandExecutionHookDefault.GetInstance());
                }
                return commandExecutionHook.Value;
            }
        }

        public static void RegisterMetricsPublisher(HystrixCommandExecutionHook impl)
        {
            if (!commandExecutionHook.CompareAndSet(null, impl))
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
                if (options.Value == null)
                {
                    options.CompareAndSet(null, HystrixOptionsStrategyDefault.GetInstance());
                }
                return options.Value;
            }
        }

        public static void RegisterOptionsStrategy(HystrixOptionsStrategy impl)
        {
            if (!options.CompareAndSet(null, impl))
            {
                throw new InvalidOperationException("Another strategy was already registered.");
            }
        }
        #endregion  OptionsStrategy

        public static void Reset()
        {
            notifier.Value = null;
            concurrencyStrategy.Value = null;
            metricsPublisher.Value = null ;
            commandExecutionHook.Value = null;
            options.Value = null;
            HystrixMetricsPublisherFactory.Reset();
        }
    }
}
