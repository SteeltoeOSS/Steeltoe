// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Support;

namespace Steeltoe.Integration.Dispatcher;

/// <summary>
/// The strategy to decorate message handling tasks.
/// </summary>
public interface IMessageHandlingDecorator
{
    /// <summary>
    /// Decorate the incoming message handling runnable (task)
    /// </summary>
    /// <param name="messageHandlingRunnable">incoming message handling task</param>
    /// <returns>the newly decorated task</returns>
    IMessageHandlingRunnable Decorate(IMessageHandlingRunnable messageHandlingRunnable);
}