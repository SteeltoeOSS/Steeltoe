﻿// Copyright 2017 the original author or authors.
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

        public volatile int TickCount = 0;

        public volatile bool StreamRunning = false;

        private CountdownEvent latch;
        private ITestOutputHelper output;

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
                    var array = value as Array;
                    if (array != null)
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
            StringBuilder sb = new StringBuilder("[");
            foreach (var val in array)
            {
                sb.Append(val.ToString());
                sb.Append(v);
            }

            return sb.ToString(0, sb.Length - 1) + "]";
        }
    }
}
