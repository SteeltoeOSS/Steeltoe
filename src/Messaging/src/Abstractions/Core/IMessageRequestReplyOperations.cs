// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Core;

/// <summary>
/// Operations for sending messages to and receiving the reply from a destination.
/// </summary>
/// <typeparam name="D">the type of the destination.</typeparam>
public interface IMessageRequestReplyOperations<D>
{
    /// <summary>
    /// Send a request message and receive the reply from a default destination.
    /// </summary>
    /// <param name="requestMessage">the message to send.</param>
    /// <param name="cancellationToken">token used to signal cancelation.</param>
    /// <returns>a task to signal completion.</returns>
    Task<IMessage> SendAndReceiveAsync(IMessage requestMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a request message and receive the reply from the given destination.
    /// </summary>
    /// <param name="destination">the target destination.</param>
    /// <param name="requestMessage">the message to send.</param>
    /// <param name="cancellationToken">token used to signal cancelation.</param>
    /// <returns>a task to signal completion.</returns>
    Task<IMessage> SendAndReceiveAsync(D destination, IMessage requestMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Convert the given request object to serialized form, possibly using a message converter, send it as a message
    ///  to a default destination, receive the reply and convert its body to the specified target type.
    /// </summary>
    /// <typeparam name="T">the target type of the payload.</typeparam>
    /// <param name="request">payload for the request message to send.</param>
    /// <param name="cancellationToken">token used to signal cancelation.</param>
    /// <returns>a task to signal completion.</returns>
    Task<T> ConvertSendAndReceiveAsync<T>(object request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Convert the given request object to serialized form, possibly using a message converter, send it as a message
    ///  to a specified destination, receive the reply and convert its body to the specified target type.
    /// </summary>
    /// <typeparam name="T">the target type of the payload.</typeparam>
    /// <param name="destination">the target destination.</param>
    /// <param name="request">payload for the request message to send.</param>
    /// <param name="cancellationToken">token used to signal cancelation.</param>
    /// <returns>a task to signal completion.</returns>
    Task<T> ConvertSendAndReceiveAsync<T>(D destination, object request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Convert the given request object to serialized form, possibly using a message converter, send it as a message
    ///  to a specified destination with the given headers, receive the reply and convert its body to the specified target type.
    /// </summary>
    /// <typeparam name="T">the target type of the payload.</typeparam>
    /// <param name="destination">the target destination.</param>
    /// <param name="request">payload for the request message to send.</param>
    /// <param name="headers">the headers to send.</param>
    /// <param name="cancellationToken">token used to signal cancelation.</param>
    /// <returns>a task to signal completion.</returns>
    Task<T> ConvertSendAndReceiveAsync<T>(D destination, object request, IDictionary<string, object> headers, CancellationToken cancellationToken = default);

    /// <summary>
    /// Convert the given request object to serialized form, possibly using a message converter, send it as a message
    ///  to a default destination after applying the post processor, receive the reply and convert its body to the specified target type.
    /// </summary>
    /// <typeparam name="T">the target type of the reply.</typeparam>
    /// <param name="request">payload for the request message to send.</param>
    /// <param name="requestPostProcessor">the post processor to apply.</param>
    /// <param name="cancellationToken">token used to signal cancelation.</param>
    /// <returns>a task to signal completion.</returns>
    Task<T> ConvertSendAndReceiveAsync<T>(object request, IMessagePostProcessor requestPostProcessor, CancellationToken cancellationToken = default);

    /// <summary>
    /// Convert the given request object to serialized form, possibly using a message converter, send it as a message
    ///  to the specified destination after applying the post processor, receive the reply and convert its body to the specified target type.
    /// </summary>
    /// <typeparam name="T">the target type of the reply.</typeparam>
    /// <param name="destination">the target destination.</param>
    /// <param name="request">payload for the request message to send.</param>
    /// <param name="requestPostProcessor">the post processor to apply.</param>
    /// <param name="cancellationToken">token used to signal cancelation.</param>
    /// <returns>a task to signal completion.</returns>
    Task<T> ConvertSendAndReceiveAsync<T>(D destination, object request, IMessagePostProcessor requestPostProcessor, CancellationToken cancellationToken = default);

    /// <summary>
    /// Convert the given request object to serialized form, possibly using a message converter, send it as a message
    ///  to the specified destination after applying the post processor, with the specified headers, receive the reply and convert its body to the specified target type.
    /// </summary>
    /// <typeparam name="T">the target type of the reply.</typeparam>
    /// <param name="destination">the target destination.</param>
    /// <param name="request">payload for the request message to send.</param>
    /// <param name="headers">the headers to send.</param>
    /// <param name="requestPostProcessor">the post processor to apply.</param>
    /// <param name="cancellationToken">token used to signal cancelation.</param>
    /// <returns>a task to signal completion.</returns>
    Task<T> ConvertSendAndReceiveAsync<T>(D destination, object request, IDictionary<string, object> headers, IMessagePostProcessor requestPostProcessor, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a request message and receive the reply from a default destination.
    /// </summary>
    /// <param name="requestMessage">the message to send.</param>
    /// <returns>the receieved message; or null.</returns>
    IMessage SendAndReceive(IMessage requestMessage);

    /// <summary>
    /// Send a request message and receive the reply from the given destination.
    /// </summary>
    /// <param name="destination">the target destination.</param>
    /// <param name="requestMessage">the message to send.</param>
    /// <returns>the receieved message; or null.</returns>
    IMessage SendAndReceive(D destination, IMessage requestMessage);

    /// <summary>
    /// Convert the given request object to serialized form, possibly using a message converter, send it as a message
    ///  to a default destination, receive the reply and convert its body to the specified target type.
    /// </summary>
    /// <typeparam name="T">the target type of the payload.</typeparam>
    /// <param name="request">payload for the request message to send.</param>
    /// <returns>the receieved message; or null.</returns>
    T ConvertSendAndReceive<T>(object request);

    /// <summary>
    /// Convert the given request object to serialized form, possibly using a message converter, send it as a message
    ///  to a specified destination, receive the reply and convert its body to the specified target type.
    /// </summary>
    /// <typeparam name="T">the target type of the payload.</typeparam>
    /// <param name="destination">the target destination.</param>
    /// <param name="request">payload for the request message to send.</param>
    /// <returns>the receieved message; or null.</returns>
    T ConvertSendAndReceive<T>(D destination, object request);

    /// <summary>
    /// Convert the given request object to serialized form, possibly using a message converter, send it as a message
    ///  to a specified destination with the given headers, receive the reply and convert its body to the specified target type.
    /// </summary>
    /// <typeparam name="T">the target type of the payload.</typeparam>
    /// <param name="destination">the target destination.</param>
    /// <param name="request">payload for the request message to send.</param>
    /// <param name="headers">the headers to send.</param>
    /// <returns>the receieved message; or null.</returns>
    T ConvertSendAndReceive<T>(D destination, object request, IDictionary<string, object> headers);

    /// <summary>
    /// Convert the given request object to serialized form, possibly using a message converter, send it as a message
    ///  to a default destination after applying the post processor, receive the reply and convert its body to the specified target type.
    /// </summary>
    /// <typeparam name="T">the target type of the reply.</typeparam>
    /// <param name="request">payload for the request message to send.</param>
    /// <param name="requestPostProcessor">the post processor to apply.</param>
    /// <returns>the receieved message; or null.</returns>
    T ConvertSendAndReceive<T>(object request, IMessagePostProcessor requestPostProcessor);

    /// <summary>
    /// Convert the given request object to serialized form, possibly using a message converter, send it as a message
    ///  to the specified destination after applying the post processor, receive the reply and convert its body to the specified target type.
    /// </summary>
    /// <typeparam name="T">the target type of the reply.</typeparam>
    /// <param name="destination">the target destination.</param>
    /// <param name="request">payload for the request message to send.</param>
    /// <param name="requestPostProcessor">the post processor to apply.</param>
    /// <returns>the receieved message; or null.</returns>
    T ConvertSendAndReceive<T>(D destination, object request, IMessagePostProcessor requestPostProcessor);

    /// <summary>
    /// Convert the given request object to serialized form, possibly using a message converter, send it as a message
    ///  to the specified destination after applying the post processor, with the specified headers, receive the reply and convert its body to the specified target type.
    /// </summary>
    /// <typeparam name="T">the target type of the reply.</typeparam>
    /// <param name="destination">the target destination.</param>
    /// <param name="request">payload for the request message to send.</param>
    /// <param name="headers">the headers to send.</param>
    /// <param name="requestPostProcessor">the post processor to apply.</param>
    /// <returns>the receieved message; or null.</returns>
    T ConvertSendAndReceive<T>(D destination, object request, IDictionary<string, object> headers, IMessagePostProcessor requestPostProcessor);
}
