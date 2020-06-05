// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;

namespace Steeltoe.Integration.Support
{
    /// <summary>
    /// A contract for message decorators
    /// </summary>
    public interface IMessageDecorator
    {
        /// <summary>
        /// Process the incoming message and return the decorated result
        /// </summary>
        /// <param name="message">the message to process</param>
        /// <returns>the resulting message</returns>
        IMessage DecorateMessage(IMessage message);
    }
}
