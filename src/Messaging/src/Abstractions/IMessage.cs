// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging;

/// <summary>
/// A generic message representation with headers and a body
/// </summary>
[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public interface IMessage
{
    /// <summary>
    /// Gets the body of the message
    /// </summary>
    object Payload { get; }

    /// <summary>
    /// Gets the headers for the message
    /// </summary>
    IMessageHeaders Headers { get; }
}

/// <summary>
/// A generic message representation with headers and a body
/// </summary>
/// <typeparam name="T">the type of the payload</typeparam>
[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public interface IMessage<out T> : IMessage
{
    /// <summary>
    /// Gets the body of the message
    /// </summary>
    new T Payload { get; }
}