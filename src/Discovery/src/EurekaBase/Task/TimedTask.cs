// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
