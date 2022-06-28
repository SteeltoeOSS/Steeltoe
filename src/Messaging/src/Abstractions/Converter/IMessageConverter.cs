// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Services;
using System;

namespace Steeltoe.Messaging.Converter;

/// <summary>
/// A converter to turn the payload of a message from serialized form to a typed
/// object and vice versa.
/// </summary>
public interface IMessageConverter : IServiceNameAware
{
    /// <summary>
    /// Convert the payload of a message to a typed object.
    /// </summary>
    /// <param name="message">the input message.</param>
    /// <param name="targetClass">the target type for the conversion.</param>
    /// <returns>the result of the conversion.</returns>
    object FromMessage(IMessage message, Type targetClass);

    /// <summary>
    /// Convert the payload of a message to a typed object.
    /// </summary>
    /// <typeparam name="T">the target type for the conversion.</typeparam>
    /// <param name="message">the input message.</param>
    /// <returns>the result of the conversion.</returns>
    T FromMessage<T>(IMessage message);

    /// <summary>
    /// Create a message whose payload is the result of converting the given payload object
    /// to serialized form.
    /// </summary>
    /// <param name="payload">the object to convert.</param>
    /// <param name="headers">optional headers for the message.</param>
    /// <returns>the new message or null if converter does not support the payload type.</returns>
    IMessage ToMessage(object payload, IMessageHeaders headers);
}
