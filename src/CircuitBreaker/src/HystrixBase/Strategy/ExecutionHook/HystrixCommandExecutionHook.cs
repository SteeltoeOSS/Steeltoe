// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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