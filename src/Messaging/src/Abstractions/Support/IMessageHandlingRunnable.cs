// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;

namespace Steeltoe.Messaging.Support;

/// <summary>
/// A runnable to encapsulates a message and message handler
/// </summary>
public interface IMessageHandlingRunnable : IRunnable
{
    /// <summary>
    /// Gets the message this runnable is processing
    /// </summary>
    IMessage Message { get; }

    /// <summary>
    /// Gets the message handler that will process this message;
    /// </summary>
    IMessageHandler MessageHandler { get; }
}