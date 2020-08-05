// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;
using System.Collections.Generic;
using System.Threading;

namespace Steeltoe.Integration.Dispatcher
{
    public class RoundRobinLoadBalancingStrategy : ILoadBalancingStrategy
    {
        private int _currentHandlerIndex = -1;

        public int GetNextHandlerStartIndex(IMessage message, List<IMessageHandler> handlers)
        {
            if (handlers == null)
            {
                return 0;
            }

            var size = handlers.Count;
            if (size > 0)
            {
                var indexTail = Interlocked.Increment(ref _currentHandlerIndex) % size;
                return indexTail < 0 ? indexTail + size : indexTail;
            }
            else
            {
                return size;
            }
        }

        internal int CurrentHandlerIndex
        {
            get { return _currentHandlerIndex; }
            set { _currentHandlerIndex = value; }
        }
    }
}
