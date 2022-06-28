// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Services;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Messaging;

/// <summary>
/// An abstraction that defines methods for sending messages.
/// </summary>
public interface IMessageChannel : IServiceNameAware
{
    /// <summary>
    /// Send a message to this channel.
    /// </summary>
    /// <param name="message">the message to send.</param>
    /// <param name="cancellationToken">token used to signal cancellation.</param>
    /// <returns>a task to signal completion.</returns>
    ValueTask<bool> SendAsync(IMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a message to this channel. If the message is sent successfully,
    /// the method returns true. If the message cannot be sent due to a
    /// non-fatal reason, the method returns false. The method may also
    /// throw a Exception in case of non-recoverable errors.
    /// This method may block indefinitely, depending on the implementation.
    /// </summary>
    /// <param name="message">the message to send.</param>
    /// <returns>true if the message is sent.</returns>
    bool Send(IMessage message);

    /// <summary>
    ///  Send a message, blocking until either the message is accepted or the specified timeout period elapses.
    /// </summary>
    /// <param name="message">the message to send.</param>
    /// <param name="timeout">the timeout in milliseconds; -1 for no timeout.</param>
    /// <returns>true if the message is sent.</returns>
    bool Send(IMessage message, int timeout);
}
