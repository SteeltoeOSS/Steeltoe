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

using Steeltoe.Common.Order;
using System;

namespace Steeltoe.Messaging.Support
{
    /// <summary>
    /// Interface for interceptors that are able to view and/or modify the Messages
    /// being sent-to and/or received-from a MessageChannel
    /// </summary>
    public interface IChannelInterceptor : IOrdered
    {
        /// <summary>
        /// Invoked before the Message is actually sent to the channel.
        /// This allows for modification of the Message if necessary.
        /// </summary>
        /// <param name="message">the message being processed</param>
        /// <param name="channel">the channel the message for</param>
        /// <returns>the resulting message to send; can be null</returns>
        IMessage PreSend(IMessage message, IMessageChannel channel);

        /// <summary>
        /// Invoked immediately after the send invocation.
        /// </summary>
        /// <param name="message">the message being processed</param>
        /// <param name="channel">the channel the message for</param>
        /// <param name="sent">the return value of the send</param>
        void PostSend(IMessage message, IMessageChannel channel, bool sent);

        /// <summary>
        /// Invoked after the completion of a send regardless of any exception that
        /// have been raised thus allowing for proper resource cleanup.
        /// </summary>
        /// <param name="message">the message being processed</param>
        /// <param name="channel">the channel the message for</param>
        /// <param name="sent">the return value of the send</param>
        /// <param name="exception">the exception that may have occured; can be null</param>
        void AfterSendCompletion(IMessage message, IMessageChannel channel, bool sent, Exception exception);

        /// <summary>
        /// Invoked as soon as receive is called and before a Message is actually retrieved.
        /// </summary>
        /// <param name="channel">the channel the message for</param>
        /// <returns>false if no receive should be done;</returns>
        bool PreReceive(IMessageChannel channel);

        /// <summary>
        /// Invoked immediately after a Message has been retrieved but before it is returned to the caller.
        /// </summary>
        /// <param name="message">the message being processed</param>
        /// <param name="channel">the channel the message is from</param>
        /// <returns>the resulting message after post processing; can be null</returns>
        IMessage PostReceive(IMessage message, IMessageChannel channel);

        /// <summary>
        /// Invoked after the completion of a receive regardless of any exception that have been raised thus allowing for proper resource cleanup.
        /// </summary>
        /// <param name="message">the message being processed</param>
        /// <param name="channel">the channel the message is from</param>
        /// <param name="exception">the exception that may have occured; can be null</param>
        void AfterReceiveCompletion(IMessage message, IMessageChannel channel, Exception exception);
    }
}
