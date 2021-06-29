// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;
using System.Collections.Generic;

namespace Steeltoe.Integration.Channel
{
    /// <summary>
    /// Operations available on a channel that has queuing semantics
    /// </summary>
    public interface IQueueChannelOperations
    {
        /// <summary>
        /// Gets the size of the queue
        /// </summary>
        int QueueSize { get; }

        /// <summary>
        /// Gets the remaining capacity of the queue
        /// </summary>
        int RemainingCapacity { get; }

        /// <summary>
        /// Clear all items off the quewue
        /// </summary>
        /// <returns>list of removed messages</returns>
        IList<IMessage> Clear();

        /// <summary>
        /// Remove any Messages that are not accepted by the provided selector.
        /// </summary>
        /// <param name="messageSelector">the selector to apply</param>
        /// <returns>list of purged messages</returns>
        IList<IMessage> Purge(IMessageSelector messageSelector);
    }
}
