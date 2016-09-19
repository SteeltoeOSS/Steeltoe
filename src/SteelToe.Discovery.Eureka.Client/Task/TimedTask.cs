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

using System;
using System.Threading;

namespace Steeltoe.Discovery.Eureka.Task
{
    internal class TimedTask
    {
        public string Name { get; private set; }
        public Action Task { get; private set; }

        private int _taskRunning;

        public TimedTask(string name, Action task)
        {
            Name = name;
            Task = task;
            _taskRunning = 0;
        }

        public void Run(object state)
        {
            if (Interlocked.CompareExchange(ref _taskRunning, 1, 0) == 0)
            {
                try
                {
                    Task();
                }
                catch (Exception)
                {
                    // Log
                }
                Interlocked.Exchange(ref _taskRunning, 0);
            }
            else
            {
                // Log, already running
            }
        }
    }
}
