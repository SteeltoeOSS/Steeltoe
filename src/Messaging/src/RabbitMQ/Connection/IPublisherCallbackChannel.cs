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

using RabbitMQ.Client;
using System;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Rabbit.Connection
{
    public interface IPublisherCallbackChannel : IModel
    {
        /// <summary>
        /// Add a publisher callback listener
        /// </summary>
        /// <param name="listener">the listener to add</param>
        void AddListener(IListener listener);

        /// <summary>
        /// Expire (remove) any pending confirmations created before the specified cutoff time
        /// for the supplied listener and return them to the caller
        /// </summary>
        /// <param name="listener">the listener</param>
        /// <param name="cutoffTime">the time before which expired messages were created</param>
        /// <returns>the list of expired confirms</returns>
        IList<PendingConfirm> Expire(IListener listener, long cutoffTime);

        /// <summary>
        /// Get the total pending confirm count for the listener
        /// </summary>
        /// <param name="listener">the listener to get confirm count for</param>
        /// <returns>the count of pending confirms</returns>
        int GetPendingConfirmsCount(IListener listener);

        /// <summary>
        /// Gett the total pending confirm count
        /// </summary>
        /// <returns>the total count</returns>
        int GetPendingConfirmsCount();

        /// <summary>
        /// Add a pending confirmation to this channels map
        /// </summary>
        /// <param name="listener">the listener the pending confir is for</param>
        /// <param name="sequence">the key to the map</param>
        /// <param name="pendingConfirm">the pending confirm</param>
        void AddPendingConfirm(IListener listener, ulong sequence, PendingConfirm pendingConfirm);

        /// <summary>
        /// Gets the underlying RabbitMQ model
        /// </summary>
        RabbitMQ.Client.IModel Channel { get; }

        /// <summary>
        /// Set a callback to be invoked after the ack/nack has been handled
        /// </summary>
        /// <param name="callback">the callback</param>
        void SetAfterAckCallback(Action<IModel> callback);

        public interface IListener
        {
            /// <summary>
            /// Invoked by the channel when a confirmation is received
            /// </summary>
            /// <param name="pendingConfirm">the pending confirmation</param>
            /// <param name="ack">true when an ack; false when a nack</param>
            void HandleConfirm(PendingConfirm pendingConfirm, bool ack);

            /// <summary>
            /// Invoked when a basic return command is received
            /// </summary>
            /// <param name="replyCode">the reason code of the return</param>
            /// <param name="replyText">the text from the broker describing the return</param>
            /// <param name="exchange">the exchange the returned message was originally published to.</param>
            /// <param name="routingKey">the routing key used when the message was originally published.</param>
            /// <param name="properties">the content header of the message.</param>
            /// <param name="body">the body of the message</param>
            void HandleReturn(int replyCode, string replyText, string exchange, string routingKey, IBasicProperties properties, byte[] body);

            /// <summary>
            /// When called this listener should remove all references to the channel
            /// </summary>
            /// <param name="channel">the channel</param>
            void Revoke(IModel channel);

            /// <summary>
            /// Gets the UUID used to identify this listener for returns
            /// </summary>
            string UUID { get; }

            /// <summary>
            /// Gets a value indicating whether this is a confirm listener
            /// </summary>
            bool IsConfirmListener { get; }

            /// <summary>
            /// Gets a value indicating whether this is a returns listener
            /// </summary>
            bool IsReturnListener { get; }
        }
    }
}
