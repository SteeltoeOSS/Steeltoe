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

using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using System;

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.ExecutionHook
{
    public abstract class HystrixCommandExecutionHook
    {
        public virtual void OnStart(IHystrixInvokable commandInstance)
        {
            // do nothing by default
        }

        public virtual T OnEmit<T>(IHystrixInvokable commandInstance, T value)
        {
            return value; // by default, just pass through
        }

        public virtual Exception OnError(IHystrixInvokable commandInstance, FailureType failureType, Exception e)
        {
            return e; // by default, just pass through
        }

        public virtual void OnSuccess(IHystrixInvokable commandInstance)
        {
            // do nothing by default
        }

        public virtual void OnThreadStart(IHystrixInvokable commandInstance)
        {
            // do nothing by default
        }

        public virtual void OnThreadComplete(IHystrixInvokable commandInstance)
        {
            // do nothing by default
        }

        public virtual void OnExecutionStart(IHystrixInvokable commandInstance)
        {
            // do nothing by default
        }

        public virtual T OnExecutionEmit<T>(IHystrixInvokable commandInstance, T value)
        {
            return value; // by default, just pass through
        }

        public virtual Exception OnExecutionError(IHystrixInvokable commandInstance, Exception e)
        {
            return e; // by default, just pass through
        }

        public virtual void OnExecutionSuccess(IHystrixInvokable commandInstance)
        {
            // do nothing by default
        }

        public virtual void OnFallbackStart(IHystrixInvokable commandInstance)
        {
            // do nothing by default
        }

        public virtual T OnFallbackEmit<T>(IHystrixInvokable commandInstance, T value)
        {
            return value; // by default, just pass through
        }

        public virtual Exception OnFallbackError(IHystrixInvokable commandInstance, Exception e)
        {
            // by default, just pass through
            return e;
        }

        public virtual void OnFallbackSuccess(IHystrixInvokable commandInstance)
        {
            // do nothing by default
        }

        public virtual void OnCacheHit(IHystrixInvokable commandInstance)
        {
            // do nothing by default
        }
    }
}