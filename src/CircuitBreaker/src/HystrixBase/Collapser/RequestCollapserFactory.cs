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

using Steeltoe.CircuitBreaker.Hystrix.Strategy;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using Steeltoe.CircuitBreaker.Util;
using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix.Collapser
{
    public class RequestCollapserFactory
    {
        private readonly ICollapserTimer timer;
        private readonly HystrixConcurrencyStrategy concurrencyStrategy;

        public RequestCollapserFactory(IHystrixCollapserKey collapserKey, RequestCollapserScope scope, ICollapserTimer timer, IHystrixCollapserOptions properties)
        {
            /* strategy: ConcurrencyStrategy */
            concurrencyStrategy = HystrixPlugins.ConcurrencyStrategy;
            this.timer = timer;
            Scope = scope;
            CollapserKey = collapserKey;
            Properties = properties;
        }

        public static void Reset()
        {
            globalScopedCollapsers.Clear();
            requestScopedCollapsers.Clear();
            HystrixTimer.Reset();
        }

        internal static void ResetRequest()
        {
            requestScopedCollapsers.Clear();
        }

        internal static RequestCollapserRequestVariable<BatchReturnType, ResponseType, RequestArgumentType> GetRequestVariable<BatchReturnType, ResponseType, RequestArgumentType>(string key)
        {
            if (!requestScopedCollapsers.TryGetValue(key, out object result))
            {
                return null;
            }

            return (RequestCollapserRequestVariable<BatchReturnType, ResponseType, RequestArgumentType>)result;
        }

        public IHystrixCollapserKey CollapserKey { get; }

        public RequestCollapserScope Scope { get; }

        public IHystrixCollapserOptions Properties { get; }

        public RequestCollapser<BatchReturnType, ResponseType, RequestArgumentType> GetRequestCollapser<BatchReturnType, ResponseType, RequestArgumentType>(HystrixCollapser<BatchReturnType, ResponseType, RequestArgumentType> commandCollapser)
        {
            if (Scope == RequestCollapserScope.REQUEST)
            {
                return GetCollapserForUserRequest(commandCollapser);
            }
            else if (Scope == RequestCollapserScope.GLOBAL)
            {
                return GetCollapserForGlobalScope(commandCollapser);
            }
            else
            {
                // logger.warn("Invalid Scope: {}  Defaulting to REQUEST scope.", getScope());
                return GetCollapserForUserRequest(commandCollapser);
            }
        }

        // String is CollapserKey.name() (we can't use CollapserKey directly as we can't guarantee it implements hashcode/equals correctly)
        private static ConcurrentDictionary<string, object> globalScopedCollapsers = new ConcurrentDictionary<string, object>();

        private RequestCollapser<BatchReturnType, ResponseType, RequestArgumentType> GetCollapserForGlobalScope<BatchReturnType, ResponseType, RequestArgumentType>(HystrixCollapser<BatchReturnType, ResponseType, RequestArgumentType> commandCollapser)
        {
            var result = globalScopedCollapsers.GetOrAddEx(CollapserKey.Name, (k) => new RequestCollapser<BatchReturnType, ResponseType, RequestArgumentType>(commandCollapser, Properties, timer, concurrencyStrategy));
            return (RequestCollapser<BatchReturnType, ResponseType, RequestArgumentType>)result;
        }

        // String is HystrixCollapserKey.name() (we can't use HystrixCollapserKey directly as we can't guarantee it implements hashcode/equals correctly)
        private static ConcurrentDictionary<string, object> requestScopedCollapsers = new ConcurrentDictionary<string, object>();

        private RequestCollapser<BatchReturnType, ResponseType, RequestArgumentType> GetCollapserForUserRequest<BatchReturnType, ResponseType, RequestArgumentType>(HystrixCollapser<BatchReturnType, ResponseType, RequestArgumentType> commandCollapser)
        {
            return GetRequestVariableForCommand(commandCollapser).Value;
        }

        private RequestCollapserRequestVariable<BatchReturnType, ResponseType, RequestArgumentType> GetRequestVariableForCommand<BatchReturnType, ResponseType, RequestArgumentType>(HystrixCollapser<BatchReturnType, ResponseType, RequestArgumentType> commandCollapser)
        {
            var result = requestScopedCollapsers.GetOrAddEx(commandCollapser.CollapserKey.Name, (k) => new RequestCollapserRequestVariable<BatchReturnType, ResponseType, RequestArgumentType>(commandCollapser, Properties, timer, concurrencyStrategy));
            return (RequestCollapserRequestVariable<BatchReturnType, ResponseType, RequestArgumentType>)result;
        }

        internal class RequestCollapserRequestVariable<BatchReturnType, ResponseType, RequestArgumentType> : HystrixRequestVariableDefault<RequestCollapser<BatchReturnType, ResponseType, RequestArgumentType>>
        {
            public RequestCollapserRequestVariable(HystrixCollapser<BatchReturnType, ResponseType, RequestArgumentType> commandCollapser, IHystrixCollapserOptions properties, ICollapserTimer timer, HystrixConcurrencyStrategy concurrencyStrategy)
                : base(() => new RequestCollapser<BatchReturnType, ResponseType, RequestArgumentType>(commandCollapser, properties, timer, concurrencyStrategy), (collapser) => collapser.Shutdown())
            {
            }
        }
    }
}
