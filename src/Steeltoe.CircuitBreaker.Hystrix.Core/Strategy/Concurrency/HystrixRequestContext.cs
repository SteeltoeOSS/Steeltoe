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

using System;
using System.Collections.Concurrent;
using System.Threading;


namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency
{
    public class HystrixRequestContext : IDisposable
    {
        private static readonly AsyncLocal<HystrixRequestContext> requestVariables = new AsyncLocal<HystrixRequestContext>();

        internal ConcurrentDictionary<IDisposable, object> State { get;  set; }

        private HystrixRequestContext()
        {
        }

        public static bool IsCurrentThreadInitialized
        {
            get
            {
                HystrixRequestContext context = requestVariables.Value;
                return context != null && context.State != null;
            }
        }

        public static HystrixRequestContext ContextForCurrentThread
        {
            get
            {
       
                if (IsCurrentThreadInitialized)
                {
                    return requestVariables.Value; 
                }
                else
                {
                    return null;
                }
            }
        }
        public static void SetContextOnCurrentThread(HystrixRequestContext state)
        {
            requestVariables.Value = state;
        }


        public static HystrixRequestContext InitializeContext()
        {
            HystrixRequestContext context = new HystrixRequestContext();
            context.State = new ConcurrentDictionary<IDisposable, object>();
            requestVariables.Value = context;
            return context;
        }


        public virtual void Dispose()
        {
            if (State != null)
            {

                foreach (var v in State.Keys)
                {
                    try
                    {
                        v.Dispose();

                        object oldValue;
                        State.TryRemove(v, out oldValue);
              
                    }
                    catch (Exception )
                    {
                        //HystrixRequestVariableDefault.logger.error("Error in shutdown, will continue with shutdown of other variables", t);
                    }
                }

                State = null;
            }
        }

    }
}
