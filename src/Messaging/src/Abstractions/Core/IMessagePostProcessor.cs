// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.Core;

/// <summary>
/// A contract for processing a message after it has been created, either
/// returning a modified(effectively new) message or returning the same.
/// </summary>
public interface IMessagePostProcessor
{
    /// <summary>
    /// Process the message.
    /// </summary>
    /// <param name="message">the messate to process.</param>
    /// <returns>the result of post processing.</returns>
    IMessage PostProcessMessage(IMessage message);
}
