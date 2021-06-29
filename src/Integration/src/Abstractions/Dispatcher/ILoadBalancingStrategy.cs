// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;
using System.Collections.Generic;

namespace Steeltoe.Integration.Dispatcher
{
    /// <summary>
    /// Strategy for determining the iteration order of a MessageHandler list.
    /// </summary>
    public interface ILoadBalancingStrategy
    {
        /// <summary>
        /// Gets the next index to be used in selecting a handler from the provided list of handlers
        /// </summary>
        /// <param name="message">the message to be processed</param>
        /// <param name="handlers">the current list of handlers</param>
        /// <returns>an index into the handler list at which to start load balancing</returns>
        int GetNextHandlerStartIndex(IMessage message, List<IMessageHandler> handlers);
    }
}
