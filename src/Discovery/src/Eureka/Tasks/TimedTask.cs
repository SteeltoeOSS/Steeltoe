// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;

namespace Steeltoe.Discovery.Eureka.Tasks;

internal sealed class TimedTask
{
    private readonly Action _task;
    private int _isTaskRunning;

    public TimedTask(Action task)
    {
        ArgumentGuard.NotNull(task);

        _task = task;
        _isTaskRunning = 0;
    }

    public void Run()
    {
        if (Interlocked.CompareExchange(ref _isTaskRunning, 1, 0) == 0)
        {
            try
            {
                _task();
            }
            finally
            {
                Interlocked.Exchange(ref _isTaskRunning, 0);
            }
        }
    }
}
