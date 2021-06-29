// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency
{
    public class HystrixRequestContext : IDisposable
    {
        private static readonly AsyncLocal<HystrixRequestContext> RequestVariables = new AsyncLocal<HystrixRequestContext>();

        internal ConcurrentDictionary<IDisposable, object> State { get; set; }

        private HystrixRequestContext()
        {
        }

        public static bool IsCurrentThreadInitialized
        {
            get
            {
                var context = RequestVariables.Value;
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
                else
                {
                    return null;
                }
            }
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
            if (State != null)
            {
                foreach (var v in State.Keys)
                {
                    try
                    {
                        v.Dispose();

                        State.TryRemove(v, out var oldValue);
                    }
                    catch (Exception)
                    {
                        // HystrixRequestVariableDefault.logger.error("Error in shutdown, will continue with shutdown of other variables", t);
                    }
                }

                State = null;
            }
        }
    }
}
