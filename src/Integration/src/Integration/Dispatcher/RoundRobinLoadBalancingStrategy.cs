// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;

namespace Steeltoe.Integration.Dispatcher;

public class RoundRobinLoadBalancingStrategy : ILoadBalancingStrategy
{
    private int _currentHandlerIndex = -1;

#pragma warning disable S2292 // Trivial properties should be auto-implemented
    internal int CurrentHandlerIndex
#pragma warning restore S2292 // Trivial properties should be auto-implemented
    {
        get => _currentHandlerIndex;
        set => _currentHandlerIndex = value;
    }

    public int GetNextHandlerStartIndex(IMessage message, List<IMessageHandler> handlers)
    {
        if (handlers == null)
        {
            return 0;
        }

        int size = handlers.Count;

        if (size > 0)
        {
            int indexTail = Interlocked.Increment(ref _currentHandlerIndex) % size;
            return indexTail < 0 ? indexTail + size : indexTail;
        }

        return size;
    }
}
