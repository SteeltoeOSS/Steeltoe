// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;
using System.Reactive;
using System.Text;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Test
{
    public class TestObserverBase<T> : ObserverBase<T>
    {
        public const int STABLE_TICK_COUNT = 2;

        public volatile int TickCount;

        public volatile bool StreamRunning;

        private readonly CountdownEvent latch;
        private readonly ITestOutputHelper output;

        public TestObserverBase(ITestOutputHelper output, CountdownEvent latch)
        {
            this.latch = latch;
            this.output = output;
        }

        protected override void OnCompletedCore()
        {
            output?.WriteLine("OnComplete @ " + Time.CurrentTimeMillis + " :" + Thread.CurrentThread.ManagedThreadId);
            StreamRunning = false;
            latch.SignalEx();
        }

        protected override void OnErrorCore(Exception error)
        {
            Assert.False(true, error.Message);
        }

        protected override void OnNextCore(T value)
        {
            TickCount++;
            if (TickCount >= STABLE_TICK_COUNT)
            {
                StreamRunning = true;
            }

            if (output != null)
            {
                try
                {
                    var tostring = value.ToString();
                    if (value is Array array)
                    {
                        tostring = Join(",", array);
                    }

                    output.WriteLine("OnNext @ " + Time.CurrentTimeMillis + " :" + Thread.CurrentThread.ManagedThreadId + " : Value= " + tostring);
                    output.WriteLine("ReqLog" + "@ " + Time.CurrentTimeMillis + " : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
                }
                catch (Exception)
                {
                }
            }
        }

        private string Join(string v, Array array)
        {
            var sb = new StringBuilder("[");
            foreach (var val in array)
            {
                sb.Append(val.ToString());
                sb.Append(v);
            }

            return $"{sb.ToString(0, sb.Length - 1)}]";
        }
    }
}
