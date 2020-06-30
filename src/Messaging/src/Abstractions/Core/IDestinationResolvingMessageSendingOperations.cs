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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Core
{
    /// <summary>
    /// Provide operations for sending messages to a destination specified as a string
    /// </summary>
    /// <typeparam name="D">the type of the destination</typeparam>
    public interface IDestinationResolvingMessageSendingOperations<D> : IMessageSendingOperations<D>
    {
        /// <summary>
        /// Resolve the given destination name to a destination and send a message to it.
        /// </summary>
        /// <param name="destinationName">the destination name to resolve</param>
        /// <param name="message">the message to send</param>
        /// <param name="cancellationToken">token used to signal cancelation</param>
        /// <returns>a task to signal completion</returns>
        Task SendAsync(string destinationName, IMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolve the given destination name to a destination, convert the payload object to serialized form, possibly using a
        /// message converter, wrap it as a message and send it to the resolved destination.
        /// </summary>
        /// <param name="destinationName">the destination name to resolve</param>
        /// <param name="payload">the payload to send</param>
        /// <param name="cancellationToken">token used to signal cancelation</param>
        /// <returns>a task to signal completion</returns>
        Task ConvertAndSendAsync(string destinationName, object payload, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolve the given destination name to a destination, convert the payload object to serialized form, possibly using a
        /// message converter, wrap it as a message with the given headers, and send it to the resolved destination.
        /// </summary>
        /// <param name="destinationName">the destination name to resolve</param>
        /// <param name="payload">the payload to send</param>
        /// <param name="headers">the headers to send</param>
        /// <param name="cancellationToken">token used to signal cancelation</param>
        /// <returns>a task to signal completion</returns>
        Task ConvertAndSendAsync(string destinationName, object payload, IDictionary<string, object> headers, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolve the given destination name to a destination, convert the payload object to serialized form, possibly using a
        /// message converter, wrap it as a message, apply the post processor, and send it to the resolved destination.
        /// </summary>
        /// <param name="destinationName">the destination name to resolve</param>
        /// <param name="payload">the payload to send</param>
        /// <param name="postProcessor">the post processor to apply</param>
        /// <param name="cancellationToken">token used to signal cancelation</param>
        /// <returns>a task to signal completion</returns>
        Task ConvertAndSendAsync(string destinationName, object payload, IMessagePostProcessor postProcessor, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolve the given destination name to a destination, convert the payload object to serialized form, possibly using a
        /// message converter, wrap it as a message with the given headers, apply the post processor and send it to the resolved destination.
        /// </summary>
        /// <param name="destinationName">the destination name to resolve</param>
        /// <param name="payload">the payload to send</param>
        /// <param name="headers">the headers to send</param>
        /// <param name="postProcessor">the post processor to apply</param>
        /// <param name="cancellationToken">token used to signal cancelation</param>
        /// <returns>a task to signal completion</returns>
        Task ConvertAndSendAsync(string destinationName, object payload, IDictionary<string, object> headers, IMessagePostProcessor postProcessor, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolve the given destination name to a destination and send a message to it.
        /// </summary>
        /// <param name="destinationName">the destination name to resolve</param>
        /// <param name="message">the message to send</param>
        void Send(string destinationName, IMessage message);

        /// <summary>
        /// Resolve the given destination name to a destination, convert the payload object to serialized form, possibly using a
        /// message converter, wrap it as a message and send it to the resolved destination.
        /// </summary>
        /// <param name="destinationName">the destination name to resolve</param>
        /// <param name="payload">the payload to send</param>
        void ConvertAndSend(string destinationName, object payload);

        /// <summary>
        /// Resolve the given destination name to a destination, convert the payload object to serialized form, possibly using a
        /// message converter, wrap it as a message with the given headers, and send it to the resolved destination.
        /// </summary>
        /// <param name="destinationName">the destination name to resolve</param>
        /// <param name="payload">the payload to send</param>
        /// <param name="headers">the headers to send</param>
        void ConvertAndSend(string destinationName, object payload, IDictionary<string, object> headers);

        /// <summary>
        /// Resolve the given destination name to a destination, convert the payload object to serialized form, possibly using a
        /// message converter, wrap it as a message, apply the post processor, and send it to the resolved destination.
        /// </summary>
        /// <param name="destinationName">the destination name to resolve</param>
        /// <param name="payload">the payload to send</param>
        /// <param name="postProcessor">the post processor to apply</param>
        void ConvertAndSend(string destinationName, object payload, IMessagePostProcessor postProcessor);

        /// <summary>
        /// Resolve the given destination name to a destination, convert the payload object to serialized form, possibly using a
        /// message converter, wrap it as a message with the given headers, apply the post processor and send it to the resolved destination.
        /// </summary>
        /// <param name="destinationName">the destination name to resolve</param>
        /// <param name="payload">the payload to send</param>
        /// <param name="headers">the headers to send</param>
        /// <param name="postProcessor">the post processor to apply</param>
        void ConvertAndSend(string destinationName, object payload, IDictionary<string, object> headers, IMessagePostProcessor postProcessor);
    }
}
