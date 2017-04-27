//
// Copyright 2015 the original author or authors.
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

using Steeltoe.Discovery.Eureka.Client.Test;
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
            System.Threading.Thread.Sleep(1000);
            Assert.Equal(1, _timerFuncCount);

            timer.Dispose();
        }


        private volatile int _timerFuncCount;
        public void TimerFunc()
        {
            ++_timerFuncCount;
            System.Threading.Thread.Sleep(3000);
        }

    }
}
