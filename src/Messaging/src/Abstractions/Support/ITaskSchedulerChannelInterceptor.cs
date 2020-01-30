// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;

namespace Steeltoe.Messaging.Support
{
    /// <summary>
    /// A specialized ChannelInterceptor for TaskScheduler based channels
    /// </summary>
    public interface ITaskSchedulerChannelInterceptor : IChannelInterceptor
    {
        /// <summary>
        /// Invoked inside the Task submitted to the Scheduler just before calling the target MessageHandler to handle the message.
        /// </summary>
        /// <param name="message">the message to be handled</param>
        /// <param name="channel">the channel the message is for</param>
        /// <param name="handler">the target handler to handle the message</param>
        /// <returns>the processed message; can be new message or null</returns>
        IMessage BeforeHandled(IMessage message, IMessageChannel channel, IMessageHandler handler);

        /// <summary>
        /// Invoked inside the Task submitted to the Scheduler after calling the target MessageHandler regardless
        /// of the outcome (i.e.Exception raised or not) thus allowing for proper resource cleanup.
        /// </summary>
        /// <param name="message">the message to be handled</param>
        /// <param name="channel">the channel the message is for</param>
        /// <param name="handler">the target handler to handle the message</param>
        /// <param name="exception">any exception that might have occured</param>
        void AfterMessageHandled(IMessage message, IMessageChannel channel, IMessageHandler handler, Exception exception);
    }
}
