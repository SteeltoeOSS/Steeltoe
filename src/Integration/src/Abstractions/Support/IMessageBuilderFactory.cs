// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;

namespace Steeltoe.Integration.Support;

/// <summary>
/// A factory for creating message builders
/// </summary>
public interface IMessageBuilderFactory
{
    /// <summary>
    /// Create a message builder from the given message
    /// </summary>
    /// <typeparam name="T">the type of payload</typeparam>
    /// <param name="message">the message to use</param>
    /// <returns>the message builder</returns>
    IMessageBuilder<T> FromMessage<T>(IMessage<T> message);

    /// <summary>
    /// Create a message builder from the given message
    /// </summary>
    /// <param name="message">the message to use</param>
    /// <returns>the message builder</returns>
    IMessageBuilder FromMessage(IMessage message);

    /// <summary>
    /// Create a message builder from the given message payload
    /// </summary>
    /// <typeparam name="T">the type of the payload</typeparam>
    /// <param name="payload">the payload of the message</param>
    /// <returns>the message builder</returns>
    IMessageBuilder<T> WithPayload<T>(T payload);

    /// <summary>
    /// Create a message builder from the given message payload
    /// </summary>
    /// <param name="payload">the payload of the message</param>
    /// <returns>the message builder</returns>
    IMessageBuilder WithPayload(object payload);
}
