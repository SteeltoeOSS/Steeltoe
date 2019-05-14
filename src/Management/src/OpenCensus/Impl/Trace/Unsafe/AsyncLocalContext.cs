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
using System.Collections.Generic;
using System.Threading;

namespace Steeltoe.Management.Census.Trace.Unsafe
{
    [Obsolete("Use OpenCensus project packages")]
    public static class AsyncLocalContext
    {
        private static List<IAsyncLocalContextListener> callbacks = new List<IAsyncLocalContextListener>();

        private static AsyncLocal<ISpan> _context = new AsyncLocal<ISpan>((arg) =>
        {
            CallListeners(arg);

            // var context = Thread.CurrentThread.ExecutionContext;
            // Console.WriteLine(context.GetHashCode());
            // Console.WriteLine("Value={0}, ThreadId={1}, TaskId={2}", ((Span)_context.Value)?.Context.SpanId, Thread.CurrentThread.ManagedThreadId, Task.CurrentId);
            // Console.WriteLine("newValue={0}, oldValue={1}, ContextChange={2}, ThreadId={3}, TaskId={4}",
            //    ((Span)arg.CurrentValue)?.Context.SpanId, ((Span)arg.PreviousValue)?.Context.SpanId, arg.ThreadContextChanged,
            //    Thread.CurrentThread.ManagedThreadId, Task.CurrentId);
            // System.Diagnostics.StackTrace t = new System.Diagnostics.StackTrace();
            // Console.WriteLine(t.ToString());
        });

        public static ISpan CurrentSpan
        {
            get
            {
                return _context.Value;
            }

            set
            {
                _context.Value = value;
            }
        }

        public static void AddListener(IAsyncLocalContextListener listener)
        {
            callbacks.Add(listener);
        }

        public static bool RemoveListener(IAsyncLocalContextListener listener)
        {
            return callbacks.Remove(listener);
        }

        private static void CallListeners(AsyncLocalValueChangedArgs<ISpan> args)
        {
            foreach (var callback in callbacks)
            {
                try
                {
                    callback.ContextChanged(args.PreviousValue, args.CurrentValue, args.ThreadContextChanged);
                }
                catch (Exception)
                {
                    // Log
                }
            }
        }
    }
}
