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
    /// Provide operations for sending and receiving messages to and from a destination specified as a string
    /// </summary>
    /// <typeparam name="D">the type of the destination</typeparam>
    public interface IDestinationResolvingMessageRequestReplyOperations<D> : IMessageRequestReplyOperations<D>
    {
        /// <summary>
        /// Resolve the given destination name to a destination and send the given message,
        /// receive a reply and return it.
        /// </summary>
        /// <param name="destinationName">the name of the target destination</param>
        /// <param name="requestMessage">the message to send</param>
        /// <param name="cancellationToken">token used to signal cancelation</param>
        /// <returns>a task to signal completion</returns>
        Task<IMessage> SendAndReceiveAsync(string destinationName, IMessage requestMessage, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolve the given destination name, convert the payload request object to serialized form, possibly using a
        /// message converter and then wrap it as a message and send it to the resolved destination, receive a reply
        /// and convert its body to the specified target type.
        /// </summary>
        /// <typeparam name="T">the type of the reply</typeparam>
        /// <param name="destinationName">the name of the target destination</param>
        /// <param name="request">the payload for the request message</param>
        /// <param name="cancellationToken">token used to signal cancelation</param>
        /// <returns>a task to signal completion</returns>
        Task<T> ConvertSendAndReceiveAsync<T>(string destinationName, object request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolve the given destination name, convert the payload request object to serialized form, possibly using a
        /// message converter and then wrap it as a message with the given headers and send it to the resolved destination, receive a reply
        /// and convert its body to the specified target type.
        /// </summary>
        /// <typeparam name="T">the type of the reply</typeparam>
        /// <param name="destinationName">the name of the target destination</param>
        /// <param name="request">the payload for the request message</param>
        /// <param name="headers">the headers to include in the message</param>
        /// <param name="cancellationToken">token used to signal cancelation</param>
        /// <returns>a task to signal completion</returns>
        Task<T> ConvertSendAndReceiveAsync<T>(string destinationName, object request, IDictionary<string, object> headers, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolve the given destination name, convert the payload request object to serialized form, possibly using a
        /// message converter and then wrap it as a message, apply the post process, and send it to the resolved destination, receive a reply
        /// and convert its body to the specified target type.
        /// </summary>
        /// <typeparam name="T">the type of the reply</typeparam>
        /// <param name="destinationName">the name of the target destination</param>
        /// <param name="request">the payload for the request message</param>
        /// <param name="requestPostProcessor">post process for the request message</param>
        /// <param name="cancellationToken">token used to signal cancelation</param>
        /// <returns>a task to signal completion</returns>
        Task<T> ConvertSendAndReceiveAsync<T>(string destinationName, object request, IMessagePostProcessor requestPostProcessor, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolve the given destination name, convert the payload request object to serialized form, possibly using a
        /// message converter and then wrap it as a message with the given headers, apply the post process, and send it to the resolved destination, receive a reply
        /// and convert its body to the specified target type.
        /// </summary>
        /// <typeparam name="T">the type of the reply</typeparam>
        /// <param name="destinationName">the name of the target destination</param>
        /// <param name="request">the payload for the request message</param>
        /// <param name="headers">the headers to include in the message</param>
        /// <param name="requestPostProcessor">post process for the request message</param>
        /// <param name="cancellationToken">token used to signal cancelation</param>
        /// <returns>a task to signal completion</returns>
        Task<T> ConvertSendAndReceiveAsync<T>(string destinationName, object request, IDictionary<string, object> headers, IMessagePostProcessor requestPostProcessor, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolve the given destination name to a destination and send the given message,
        /// receive a reply and return it.
        /// </summary>
        /// <param name="destinationName">the name of the target destination</param>
        /// <param name="requestMessage">the message to send</param>
        /// <returns>the received message or null if nothing received</returns>
        IMessage SendAndReceive(string destinationName, IMessage requestMessage);

        /// <summary>
        /// Resolve the given destination name, convert the payload request object to serialized form, possibly using a
        /// message converter and then wrap it as a message and send it to the resolved destination, receive a reply
        /// and convert its body to the specified target type.
        /// </summary>
        /// <typeparam name="T">the type of the reply</typeparam>
        /// <param name="destinationName">the name of the target destination</param>
        /// <param name="request">the payload for the request message</param>
        /// <returns>the converted payload of the reply message, possibly null</returns>
        T ConvertSendAndReceive<T>(string destinationName, object request);

        /// <summary>
        /// Resolve the given destination name, convert the payload request object to serialized form, possibly using a
        /// message converter and then wrap it as a message with the given headers and send it to the resolved destination, receive a reply
        /// and convert its body to the specified target type.
        /// </summary>
        /// <typeparam name="T">the type of the reply</typeparam>
        /// <param name="destinationName">the name of the target destination</param>
        /// <param name="request">the payload for the request message</param>
        /// <param name="headers">the headers to include in the message</param>
        /// <returns>the converted payload of the reply message, possibly null</returns>
        T ConvertSendAndReceive<T>(string destinationName, object request, IDictionary<string, object> headers);

        /// <summary>
        /// Resolve the given destination name, convert the payload request object to serialized form, possibly using a
        /// message converter and then wrap it as a message, apply the post process, and send it to the resolved destination, receive a reply
        /// and convert its body to the specified target type.
        /// </summary>
        /// <typeparam name="T">the type of the reply</typeparam>
        /// <param name="destinationName">the name of the target destination</param>
        /// <param name="request">the payload for the request message</param>
        /// <param name="requestPostProcessor">post process for the request message</param>
        /// <returns>the converted payload of the reply message, possibly null</returns>
        T ConvertSendAndReceive<T>(string destinationName, object request, IMessagePostProcessor requestPostProcessor);

        /// <summary>
        /// Resolve the given destination name, convert the payload request object to serialized form, possibly using a
        /// message converter and then wrap it as a message with the given headers, apply the post process, and send it to the resolved destination, receive a reply
        /// and convert its body to the specified target type.
        /// </summary>
        /// <typeparam name="T">the type of the reply</typeparam>
        /// <param name="destinationName">the name of the target destination</param>
        /// <param name="request">the payload for the request message</param>
        /// <param name="headers">the headers to include in the message</param>
        /// <param name="requestPostProcessor">post process for the request message</param>
        /// <returns>the converted payload of the reply message, possibly null</returns>
        T ConvertSendAndReceive<T>(string destinationName, object request, IDictionary<string, object> headers, IMessagePostProcessor requestPostProcessor);
    }
}
