// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging;

/// <summary>
/// Simple contract for handling a Message.
/// </summary>
public interface IAsyncMessageHandler
{
    /// <summary>
    /// Handle the given method.
    /// </summary>
    /// <param name="message">the message to process.</param>
    /// <param name="cancellationToken">token used to signal cancellation.</param>
    /// <returns>a task to signal completion.</returns>
    Task HandleMessage(IMessage message, CancellationToken cancellationToken = default);
}
