// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Messaging.Support;

/// <summary>
/// A specialized ChannelInterceptor for TaskScheduler based channels.
/// </summary>
public interface ITaskSchedulerChannelInterceptor : IChannelInterceptor
{
    /// <summary>
    /// Invoked inside the Task submitted to the Scheduler just before calling the target MessageHandler to handle the message.
    /// </summary>
    /// <param name="message">the message to be handled.</param>
    /// <param name="channel">the channel the message is for.</param>
    /// <param name="handler">the target handler to handle the message.</param>
    /// <returns>the processed message; can be new message or null.</returns>
    IMessage BeforeHandled(IMessage message, IMessageChannel channel, IMessageHandler handler);

    /// <summary>
    /// Invoked inside the Task submitted to the Scheduler after calling the target MessageHandler regardless
    /// of the outcome (i.e.Exception raised or not) thus allowing for proper resource cleanup.
    /// </summary>
    /// <param name="message">the message to be handled.</param>
    /// <param name="channel">the channel the message is for.</param>
    /// <param name="handler">the target handler to handle the message.</param>
    /// <param name="exception">any exception that might have occured.</param>
    void AfterMessageHandled(IMessage message, IMessageChannel channel, IMessageHandler handler, Exception exception);
}
