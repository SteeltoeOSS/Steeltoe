// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Core;

/// <summary>
/// Operations for receiving messages from a destination.
/// </summary>
/// <typeparam name="D">the type of the destination</typeparam>
public interface IMessageReceivingOperations<D>
{
    /// <summary>
    /// Receive a message from a default destination
    /// </summary>
    /// <param name="cancellationToken">token used to signal cancelation</param>
    /// <returns>a task to signal completion</returns>
    Task<IMessage> ReceiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Receive a message from the given destination
    /// </summary>
    /// <param name="destination">the target destination</param>
    /// <param name="cancellationToken">token used to signal cancelation</param>
    /// <returns>a task to signal completion</returns>
    Task<IMessage> ReceiveAsync(D destination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Receive a message from a default destination and convert its payload to the specified target type.
    /// </summary>
    /// <typeparam name="T">the type of the payload</typeparam>
    /// <param name="cancellationToken">token used to signal cancelation</param>
    /// <returns>a task to signal completion</returns>
    Task<T> ReceiveAndConvertAsync<T>(CancellationToken cancellationToken = default);

    /// <summary>
    /// Receive a message from the given destination and convert its payload to the specified target type.
    /// </summary>
    /// <typeparam name="T">the type of the payload</typeparam>
    /// <param name="destination">the target destination</param>
    /// <param name="cancellationToken">token used to signal cancelation</param>
    /// <returns>a task to signal completion</returns>
    Task<T> ReceiveAndConvertAsync<T>(D destination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Receive a message from a default destination
    /// </summary>
    /// <returns>the received message; or null</returns>
    IMessage Receive();

    /// <summary>
    /// Receive a message from the given destination
    /// </summary>
    /// <param name="destination">the target destination</param>
    /// <returns>the received message; or null</returns>
    IMessage Receive(D destination);

    /// <summary>
    /// Receive a message from a default destination and convert its payload to the specified target type.
    /// </summary>
    /// <typeparam name="T">the type of the payload</typeparam>
    /// <returns>the received message; or null</returns>
    T ReceiveAndConvert<T>();

    /// <summary>
    /// Receive a message from the given destination and convert its payload to the specified target type.
    /// </summary>
    /// <typeparam name="T">the type of the payload</typeparam>
    /// <param name="destination">the target destination</param>
    /// <returns>the received message; or null</returns>
    T ReceiveAndConvert<T>(D destination);
}
