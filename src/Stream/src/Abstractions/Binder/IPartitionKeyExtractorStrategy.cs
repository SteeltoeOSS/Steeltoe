// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;

namespace Steeltoe.Stream.Binder
{
    /// <summary>
    /// Strategy for extracting a partition key from a Message.
    /// </summary>
    public interface IPartitionKeyExtractorStrategy
    {
        /// <summary>
        /// Gets the name of the strategy
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Extract the partition key from the incoming message
        /// </summary>
        /// <param name="message">the message to process</param>
        /// <returns>the key</returns>
        object ExtractKey(IMessage message);
    }
}
