// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.Core;

/// <summary>
/// Provide operations for sending messages to a destination specified as a string.
/// </summary>
/// <typeparam name="TDestination">
/// the type of the destination.
/// </typeparam>
public interface IDestinationResolvingMessageSendingOperations<TDestination> : IMessageSendingOperations<TDestination>
{
    /// <summary>
    /// Resolve the given destination name to a destination and send a message to it.
    /// </summary>
    /// <param name="destinationName">
    /// the destination name to resolve.
    /// </param>
    /// <param name="message">
    /// the message to send.
    /// </param>
    /// <param name="cancellationToken">
    /// token used to signal cancellation.
    /// </param>
    /// <returns>
    /// a task to signal completion.
    /// </returns>
    Task SendAsync(string destinationName, IMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve the given destination name to a destination, convert the payload object to serialized form, possibly using a message converter, wrap it as a
    /// message and send it to the resolved destination.
    /// </summary>
    /// <param name="destinationName">
    /// the destination name to resolve.
    /// </param>
    /// <param name="payload">
    /// the payload to send.
    /// </param>
    /// <param name="cancellationToken">
    /// token used to signal cancellation.
    /// </param>
    /// <returns>
    /// a task to signal completion.
    /// </returns>
    Task ConvertAndSendAsync(string destinationName, object payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve the given destination name to a destination, convert the payload object to serialized form, possibly using a message converter, wrap it as a
    /// message with the given headers, and send it to the resolved destination.
    /// </summary>
    /// <param name="destinationName">
    /// the destination name to resolve.
    /// </param>
    /// <param name="payload">
    /// the payload to send.
    /// </param>
    /// <param name="headers">
    /// the headers to send.
    /// </param>
    /// <param name="cancellationToken">
    /// token used to signal cancellation.
    /// </param>
    /// <returns>
    /// a task to signal completion.
    /// </returns>
    Task ConvertAndSendAsync(string destinationName, object payload, IDictionary<string, object> headers, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve the given destination name to a destination, convert the payload object to serialized form, possibly using a message converter, wrap it as a
    /// message, apply the post processor, and send it to the resolved destination.
    /// </summary>
    /// <param name="destinationName">
    /// the destination name to resolve.
    /// </param>
    /// <param name="payload">
    /// the payload to send.
    /// </param>
    /// <param name="postProcessor">
    /// the post processor to apply.
    /// </param>
    /// <param name="cancellationToken">
    /// token used to signal cancellation.
    /// </param>
    /// <returns>
    /// a task to signal completion.
    /// </returns>
    Task ConvertAndSendAsync(string destinationName, object payload, IMessagePostProcessor postProcessor, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve the given destination name to a destination, convert the payload object to serialized form, possibly using a message converter, wrap it as a
    /// message with the given headers, apply the post processor and send it to the resolved destination.
    /// </summary>
    /// <param name="destinationName">
    /// the destination name to resolve.
    /// </param>
    /// <param name="payload">
    /// the payload to send.
    /// </param>
    /// <param name="headers">
    /// the headers to send.
    /// </param>
    /// <param name="postProcessor">
    /// the post processor to apply.
    /// </param>
    /// <param name="cancellationToken">
    /// token used to signal cancellation.
    /// </param>
    /// <returns>
    /// a task to signal completion.
    /// </returns>
    Task ConvertAndSendAsync(string destinationName, object payload, IDictionary<string, object> headers, IMessagePostProcessor postProcessor,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve the given destination name to a destination and send a message to it.
    /// </summary>
    /// <param name="destinationName">
    /// the destination name to resolve.
    /// </param>
    /// <param name="message">
    /// the message to send.
    /// </param>
    void Send(string destinationName, IMessage message);

    /// <summary>
    /// Resolve the given destination name to a destination, convert the payload object to serialized form, possibly using a message converter, wrap it as a
    /// message and send it to the resolved destination.
    /// </summary>
    /// <param name="destinationName">
    /// the destination name to resolve.
    /// </param>
    /// <param name="payload">
    /// the payload to send.
    /// </param>
    void ConvertAndSend(string destinationName, object payload);

    /// <summary>
    /// Resolve the given destination name to a destination, convert the payload object to serialized form, possibly using a message converter, wrap it as a
    /// message with the given headers, and send it to the resolved destination.
    /// </summary>
    /// <param name="destinationName">
    /// the destination name to resolve.
    /// </param>
    /// <param name="payload">
    /// the payload to send.
    /// </param>
    /// <param name="headers">
    /// the headers to send.
    /// </param>
    void ConvertAndSend(string destinationName, object payload, IDictionary<string, object> headers);

    /// <summary>
    /// Resolve the given destination name to a destination, convert the payload object to serialized form, possibly using a message converter, wrap it as a
    /// message, apply the post processor, and send it to the resolved destination.
    /// </summary>
    /// <param name="destinationName">
    /// the destination name to resolve.
    /// </param>
    /// <param name="payload">
    /// the payload to send.
    /// </param>
    /// <param name="postProcessor">
    /// the post processor to apply.
    /// </param>
    void ConvertAndSend(string destinationName, object payload, IMessagePostProcessor postProcessor);

    /// <summary>
    /// Resolve the given destination name to a destination, convert the payload object to serialized form, possibly using a message converter, wrap it as a
    /// message with the given headers, apply the post processor and send it to the resolved destination.
    /// </summary>
    /// <param name="destinationName">
    /// the destination name to resolve.
    /// </param>
    /// <param name="payload">
    /// the payload to send.
    /// </param>
    /// <param name="headers">
    /// the headers to send.
    /// </param>
    /// <param name="postProcessor">
    /// the post processor to apply.
    /// </param>
    void ConvertAndSend(string destinationName, object payload, IDictionary<string, object> headers, IMessagePostProcessor postProcessor);
}
