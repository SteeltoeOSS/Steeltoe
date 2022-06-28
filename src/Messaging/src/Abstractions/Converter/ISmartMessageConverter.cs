// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Messaging.Converter;

/// <summary>
/// An extended message converter supporting conversion hints.
/// </summary>
public interface ISmartMessageConverter : IMessageConverter
{
    /// <summary>
    /// Convert the payload of a message to a typed object.
    /// </summary>
    /// <param name="message">the input message.</param>
    /// <param name="targetClass">the target type of the conversion.</param>
    /// <param name="conversionHint">an extra object passed to the converter which may used for handling the conversion.</param>
    /// <returns>the result of the conversion.</returns>
    object FromMessage(IMessage message, Type targetClass, object conversionHint);

    /// <summary>
    /// Convert the payload of a message to a typed object.
    /// </summary>
    /// <typeparam name="T">the target type for the conversion.</typeparam>
    /// <param name="message">the input message.</param>
    /// <param name="conversionHint">an extra object passed to the converter which may used for handling the conversion.</param>
    /// <returns>the result of the conversion.</returns>
    T FromMessage<T>(IMessage message, object conversionHint);

    /// <summary>
    /// Create a message whose payload is the result of converting the given payload object
    /// to serialized form.
    /// </summary>
    /// <param name="payload">the object to convert.</param>
    /// <param name="headers">optional headers for the message.</param>
    /// <param name="conversionHint">an extra object passed to the converter which may used for handling the conversion.</param>
    /// <returns>the new message or null if converter does not support the payload type.</returns>
    IMessage ToMessage(object payload, IMessageHeaders headers, object conversionHint);
}
