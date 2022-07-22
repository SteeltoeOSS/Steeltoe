// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Core;

/// <summary>
/// Provides operations for receiving messages from a destination specifed as a resolvable string
/// </summary>
/// <typeparam name="D">the destination type</typeparam>
public interface IDestinationResolvingMessageReceivingOperations<D> : IMessageReceivingOperations<D>
{
    /// <summary>
    /// Resolve the given destination and receive a message from it
    /// </summary>
    /// <param name="destinationName">the destination name to resolve</param>
    /// <param name="cancellationToken">a token used to cancel the operation</param>
    /// <returns>a task to signal completion</returns>
    Task<IMessage> ReceiveAsync(string destinationName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve the given destination, receive a message from it, convert the payload to the specified target type
    /// </summary>
    /// <typeparam name="T">the target type</typeparam>
    /// <param name="destinationName">the destination name to resolve</param>
    /// <param name="cancellationToken">a token used to cancel the operation</param>
    /// <returns>a task to signal completion</returns>
    Task<T> ReceiveAndConvertAsync<T>(string destinationName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve the given destination and receive a message from it
    /// </summary>
    /// <param name="destinationName">the destination name to resolve</param>
    /// <returns>the received message</returns>
    IMessage Receive(string destinationName);

    /// <summary>
    /// Resolve the given destination, receive a message from it, convert the payload to the specified target type
    /// </summary>
    /// <typeparam name="T">the target type</typeparam>
    /// <param name="destinationName">the destination name to resolve</param>
    /// <returns>the received message</returns>
    T ReceiveAndConvert<T>(string destinationName);
}