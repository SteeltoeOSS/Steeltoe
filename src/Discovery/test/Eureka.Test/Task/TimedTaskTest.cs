// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.Test;
using System.Threading;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Task.Test
{
    public class TimedTaskTest : AbstractBaseTest
    {
        [Fact]
        public void Run_Enforces_SingleActiveTask()
        {
            _timerFuncCount = 0;
            var timedTask = new TimedTask("MyTask", TimerFunc);
            var timer = new Timer(timedTask.Run, null, 10, 100);
            Thread.Sleep(1000);
            Assert.Equal(1, _timerFuncCount);

            timer.Dispose();
        }

        private volatile int _timerFuncCount;

        private void TimerFunc()
        {
            ++_timerFuncCount;
            Thread.Sleep(3000);
        }
    }
}
