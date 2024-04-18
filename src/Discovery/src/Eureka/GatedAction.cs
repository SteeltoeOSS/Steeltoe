// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;

namespace Steeltoe.Discovery.Eureka;

/// <summary>
/// Prevents running the specified action concurrently.
/// </summary>
internal sealed class GatedAction
{
    private readonly Action _action;
    private int _isRunning;

    public GatedAction(Action action)
    {
        ArgumentGuard.NotNull(action);

        _action = action;
    }

    public void Run()
    {
        if (Interlocked.CompareExchange(ref _isRunning, 1, 0) == 0)
        {
            try
            {
                _action();
            }
            finally
            {
                Interlocked.Exchange(ref _isRunning, 0);
            }
        }
    }
}
