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

using System;

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency
{
    public class HystrixConcurrencyStrategy
    {
        public virtual HystrixTaskScheduler GetTaskScheduler(IHystrixThreadPoolOptions options)
        {
            if (options.MaxQueueSize < 0)
            {
                return new HystrixSyncTaskScheduler(options);
            }
            else
            {
                return new HystrixQueuedTaskScheduler(options);
            }
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
}
