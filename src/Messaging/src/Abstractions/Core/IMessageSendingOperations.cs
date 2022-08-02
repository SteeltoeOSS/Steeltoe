// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.Core;

/// <summary>
/// Operations for sending messages to a destination.
/// </summary>
/// <typeparam name="TDestination">
/// the type of the destination.
/// </typeparam>
public interface IMessageSendingOperations<TDestination>
{
    /// <summary>
    /// Send a message to a default destination.
    /// </summary>
    /// <param name="message">
    /// the message to send.
    /// </param>
    /// <param name="cancellationToken">
    /// token used to signal cancellation.
    /// </param>
    /// <returns>
    /// a task to signal completion.
    /// </returns>
    Task SendAsync(IMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a message to the given destination.
    /// </summary>
    /// <param name="destination">
    /// the target destination.
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
    Task SendAsync(TDestination destination, IMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Convert the given object to serialized form, possibly using a message converter, wrap it as a message and send it to a default destination.
    /// </summary>
    /// <param name="payload">
    /// the payload to send.
    /// </param>
    /// <param name="cancellationToken">
    /// token used to signal cancellation.
    /// </param>
    /// <returns>
    /// a task to signal completion.
    /// </returns>
    Task ConvertAndSendAsync(object payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// Convert the given object to serialized form, possibly using a message converter, wrap it as a message and send it to a specified destination.
    /// </summary>
    /// <param name="destination">
    /// the target destination.
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
    Task ConvertAndSendAsync(TDestination destination, object payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// Convert the given object to serialized form, possibly using a message converter, wrap it as a message with the provided headers, and send it to a
    /// specified destination.
    /// </summary>
    /// <param name="destination">
    /// the target destination.
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
    Task ConvertAndSendAsync(TDestination destination, object payload, IDictionary<string, object> headers, CancellationToken cancellationToken = default);

    /// <summary>
    /// Convert the given object to serialized form, possibly using a message converter, wrap it as a message, apply the post processor, and send it to the
    /// default destination.
    /// </summary>
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
    Task ConvertAndSendAsync(object payload, IMessagePostProcessor postProcessor, CancellationToken cancellationToken = default);

    /// <summary>
    /// Convert the given object to serialized form, possibly using a message converter, wrap it as a message, apply the post processor, and send it to the
    /// specified destination.
    /// </summary>
    /// <param name="destination">
    /// the target destination.
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
    Task ConvertAndSendAsync(TDestination destination, object payload, IMessagePostProcessor postProcessor, CancellationToken cancellationToken = default);

    /// <summary>
    /// Convert the given object to serialized form, possibly using a message converter, wrap it as a message, with the provided headers, apply the post
    /// processor, and send it to the specified destination.
    /// </summary>
    /// <param name="destination">
    /// the target destination.
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
    Task ConvertAndSendAsync(TDestination destination, object payload, IDictionary<string, object> headers, IMessagePostProcessor postProcessor,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a message to a default destination.
    /// </summary>
    /// <param name="message">
    /// the message to send.
    /// </param>
    void Send(IMessage message);

    /// <summary>
    /// Send a message to the given destination.
    /// </summary>
    /// <param name="destination">
    /// the target destination.
    /// </param>
    /// <param name="message">
    /// the message to send.
    /// </param>
    void Send(TDestination destination, IMessage message);

    /// <summary>
    /// Convert the given object to serialized form, possibly using a message converter, wrap it as a message and send it to a default destination.
    /// </summary>
    /// <param name="payload">
    /// the payload to send.
    /// </param>
    void ConvertAndSend(object payload);

    /// <summary>
    /// Convert the given object to serialized form, possibly using a message converter, wrap it as a message and send it to a specified destination.
    /// </summary>
    /// <param name="destination">
    /// the target destination.
    /// </param>
    /// <param name="payload">
    /// the payload to send.
    /// </param>
    void ConvertAndSend(TDestination destination, object payload);

    /// <summary>
    /// Convert the given object to serialized form, possibly using a message converter, wrap it as a message with the provided headers, and send it to a
    /// specified destination.
    /// </summary>
    /// <param name="destination">
    /// the target destination.
    /// </param>
    /// <param name="payload">
    /// the payload to send.
    /// </param>
    /// <param name="headers">
    /// the headers to send.
    /// </param>
    void ConvertAndSend(TDestination destination, object payload, IDictionary<string, object> headers);

    /// <summary>
    /// Convert the given object to serialized form, possibly using a message converter, wrap it as a message, apply the post processor, and send it to the
    /// default destination.
    /// </summary>
    /// <param name="payload">
    /// the payload to send.
    /// </param>
    /// <param name="postProcessor">
    /// the post processor to apply.
    /// </param>
    void ConvertAndSend(object payload, IMessagePostProcessor postProcessor);

    /// <summary>
    /// Convert the given object to serialized form, possibly using a message converter, wrap it as a message, apply the post processor, and send it to the
    /// specified destination.
    /// </summary>
    /// <param name="destination">
    /// the target destination.
    /// </param>
    /// <param name="payload">
    /// the payload to send.
    /// </param>
    /// <param name="postProcessor">
    /// the post processor to apply.
    /// </param>
    void ConvertAndSend(TDestination destination, object payload, IMessagePostProcessor postProcessor);

    /// <summary>
    /// Convert the given object to serialized form, possibly using a message converter, wrap it as a message, with the provided headers, apply the post
    /// processor, and send it to the specified destination.
    /// </summary>
    /// <param name="destination">
    /// the target destination.
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
    void ConvertAndSend(TDestination destination, object payload, IDictionary<string, object> headers, IMessagePostProcessor postProcessor);
}
