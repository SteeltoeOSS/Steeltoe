// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Messaging;

/// <summary>
/// A MessageChannel from which messages may be actively received through polling
/// </summary>
public interface IPollableChannel : IMessageChannel
{
    /// <summary>
    /// Receive a message from this channel
    /// </summary>
    /// <param name="cancellationToken">token used to signal cancelation</param>
    /// <returns>a task to signal completion</returns>
    ValueTask<IMessage> ReceiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Receive a message from this channel, blocking forever if necessary.
    /// </summary>
    /// <returns>the message</returns>
    IMessage Receive();

    /// <summary>
    /// Receive a message from this channel, blocking until either a message is available
    /// or the specified timeout period elapses.
    /// </summary>
    /// <param name="timeout">the timeout value in milliseconds</param>
    /// <returns>the message or null</returns>
    IMessage Receive(int timeout);
}