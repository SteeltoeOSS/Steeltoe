// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Discovery.Eureka.Task;

internal sealed class TimedTask
{
    private int _taskRunning;
    public string Name { get; }

    public Action Task { get; }

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
    }
}
