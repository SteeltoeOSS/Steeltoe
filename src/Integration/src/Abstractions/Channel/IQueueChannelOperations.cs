// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
