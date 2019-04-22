using Steeltoe.Management.Census.Trace;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Management.Census.Trace.Unsafe
{
    public static class AsyncLocalContext
    {
        private static List<IAsyncLocalContextListener> callbacks = new List<IAsyncLocalContextListener>();

        private static AsyncLocal<ISpan> _context = new AsyncLocal<ISpan>((arg) =>
        {
            CallListeners(arg);
            //var context = Thread.CurrentThread.ExecutionContext;
            //Console.WriteLine(context.GetHashCode());
            //Console.WriteLine("Value={0}, ThreadId={1}, TaskId={2}", ((Span)_context.Value)?.Context.SpanId, Thread.CurrentThread.ManagedThreadId, Task.CurrentId);
            //Console.WriteLine("newValue={0}, oldValue={1}, ContextChange={2}, ThreadId={3}, TaskId={4}",
            //    ((Span)arg.CurrentValue)?.Context.SpanId, ((Span)arg.PreviousValue)?.Context.SpanId, arg.ThreadContextChanged,
            //    Thread.CurrentThread.ManagedThreadId, Task.CurrentId);
            //System.Diagnostics.StackTrace t = new System.Diagnostics.StackTrace();
            //Console.WriteLine(t.ToString());
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
            foreach(var callback in callbacks)
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
