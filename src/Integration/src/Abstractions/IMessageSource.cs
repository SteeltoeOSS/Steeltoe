// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;

namespace Steeltoe.Integration;

/// <summary>
/// Base interface for any source of Messages that can be polled.
/// </summary>
public interface IMessageSource
{
    /// <summary>
    /// Poll for a message from the source.
    /// </summary>
    /// <returns>the message.</returns>
    IMessage Receive();
}

/// <summary>
/// A typed interface for any source of Messages that can be polled.
/// </summary>
/// <typeparam name="T">the type of payload in the message.</typeparam>
public interface IMessageSource<out T> : IMessageSource
{
    /// <summary>
    /// Poll for a message from the source.
    /// </summary>
    /// <returns>the message.</returns>
    new IMessage<T> Receive();
}
