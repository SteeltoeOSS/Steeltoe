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
    /// Operations for sending messages to and receiving the reply from a destination.
    /// </summary>
    /// <typeparam name="D">type type of the destination</typeparam>
    public interface IMessageRequestReplyOperations<D>
    {
        /// <summary>
        /// Send a request message and receive the reply from a default destination.
        /// </summary>
        /// <param name="requestMessage">the message to send</param>
        /// <param name="cancellationToken">token used to signal cancelation</param>
        /// <returns>a task to signal completion</returns>
        Task<IMessage> SendAndReceiveAsync(IMessage requestMessage, CancellationToken cancellationToken = default);

        /// <summary>
        /// Send a request message and receive the reply from the given destination.
        /// </summary>
        /// <param name="destination">the target destination</param>
        /// <param name="requestMessage">the message to send</param>
        /// <param name="cancellationToken">token used to signal cancelation</param>
        /// <returns>a task to signal completion</returns>
        Task<IMessage> SendAndReceiveAsync(D destination, IMessage requestMessage, CancellationToken cancellationToken = default);

        /// <summary>
        /// Convert the given request object to serialized form, possibly using a message converter, send it as a message
        ///  to a default destination, receive the reply and convert its body to the specified target type.
        /// </summary>
        /// <typeparam name="T">the target type of the payload</typeparam>
        /// <param name="request">payload for the request message to send</param>
        /// <param name="cancellationToken">token used to signal cancelation</param>
        /// <returns>a task to signal completion</returns>
        Task<T> ConvertSendAndReceiveAsync<T>(object request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Convert the given request object to serialized form, possibly using a message converter, send it as a message
        ///  to a specified destination, receive the reply and convert its body to the specified target type.
        /// </summary>
        /// <typeparam name="T">the target type of the payload</typeparam>
        /// <param name="destination">the target destination</param>
        /// <param name="request">payload for the request message to send</param>
        /// <param name="cancellationToken">token used to signal cancelation</param>
        /// <returns>a task to signal completion</returns>
        Task<T> ConvertSendAndReceiveAsync<T>(D destination, object request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Convert the given request object to serialized form, possibly using a message converter, send it as a message
        ///  to a specified destination with the given headers, receive the reply and convert its body to the specified target type.
        /// </summary>
        /// <typeparam name="T">the target type of the payload</typeparam>
        /// <param name="destination">the target destination</param>
        /// <param name="request">payload for the request message to send</param>
        /// <param name="headers">the headers to send</param>
        /// <param name="cancellationToken">token used to signal cancelation</param>
        /// <returns>a task to signal completion</returns>
        Task<T> ConvertSendAndReceiveAsync<T>(D destination, object request, IDictionary<string, object> headers, CancellationToken cancellationToken = default);

        /// <summary>
        /// Convert the given request object to serialized form, possibly using a message converter, send it as a message
        ///  to a default destination after applying the post processor, receive the reply and convert its body to the specified target type.
        /// </summary>
        /// <typeparam name="T">the target type of the reply</typeparam>
        /// <param name="request">payload for the request message to send</param>
        /// <param name="requestPostProcessor">the post processor to apply</param>
        /// <param name="cancellationToken">token used to signal cancelation</param>
        /// <returns>a task to signal completion</returns>
        Task<T> ConvertSendAndReceiveAsync<T>(object request, IMessagePostProcessor requestPostProcessor, CancellationToken cancellationToken = default);

        /// <summary>
        /// Convert the given request object to serialized form, possibly using a message converter, send it as a message
        ///  to the specified destination after applying the post processor, receive the reply and convert its body to the specified target type.
        /// </summary>
        /// <typeparam name="T">the target type of the reply</typeparam>
        /// <param name="destination">the target destination</param>
        /// <param name="request">payload for the request message to send</param>
        /// <param name="requestPostProcessor">the post processor to apply</param>
        /// <param name="cancellationToken">token used to signal cancelation</param>
        /// <returns>a task to signal completion</returns>
        Task<T> ConvertSendAndReceiveAsync<T>(D destination, object request, IMessagePostProcessor requestPostProcessor, CancellationToken cancellationToken = default);

        /// <summary>
        /// Convert the given request object to serialized form, possibly using a message converter, send it as a message
        ///  to the specified destination after applying the post processor, with the specified headers, receive the reply and convert its body to the specified target type.
        /// </summary>
        /// <typeparam name="T">the target type of the reply</typeparam>
        /// <param name="destination">the target destination</param>
        /// <param name="request">payload for the request message to send</param>
        /// <param name="headers">the headers to send</param>
        /// <param name="requestPostProcessor">the post processor to apply</param>
        /// <param name="cancellationToken">token used to signal cancelation</param>
        /// <returns>a task to signal completion</returns>
        Task<T> ConvertSendAndReceiveAsync<T>(D destination, object request, IDictionary<string, object> headers, IMessagePostProcessor requestPostProcessor, CancellationToken cancellationToken = default);

        /// <summary>
        /// Send a request message and receive the reply from a default destination.
        /// </summary>
        /// <param name="requestMessage">the message to send</param>
        /// <returns>the receieved message; or null</returns>
        IMessage SendAndReceive(IMessage requestMessage);

        /// <summary>
        /// Send a request message and receive the reply from the given destination.
        /// </summary>
        /// <param name="destination">the target destination</param>
        /// <param name="requestMessage">the message to send</param>
        /// <returns>the receieved message; or null</returns>
        IMessage SendAndReceive(D destination, IMessage requestMessage);

        /// <summary>
        /// Convert the given request object to serialized form, possibly using a message converter, send it as a message
        ///  to a default destination, receive the reply and convert its body to the specified target type.
        /// </summary>
        /// <typeparam name="T">the target type of the payload</typeparam>
        /// <param name="request">payload for the request message to send</param>
        /// <returns>the receieved message; or null</returns>
        T ConvertSendAndReceive<T>(object request);

        /// <summary>
        /// Convert the given request object to serialized form, possibly using a message converter, send it as a message
        ///  to a specified destination, receive the reply and convert its body to the specified target type.
        /// </summary>
        /// <typeparam name="T">the target type of the payload</typeparam>
        /// <param name="destination">the target destination</param>
        /// <param name="request">payload for the request message to send</param>
        /// <returns>the receieved message; or null</returns>
        T ConvertSendAndReceive<T>(D destination, object request);

        /// <summary>
        /// Convert the given request object to serialized form, possibly using a message converter, send it as a message
        ///  to a specified destination with the given headers, receive the reply and convert its body to the specified target type.
        /// </summary>
        /// <typeparam name="T">the target type of the payload</typeparam>
        /// <param name="destination">the target destination</param>
        /// <param name="request">payload for the request message to send</param>
        /// <param name="headers">the headers to send</param>
        /// <returns>the receieved message; or null</returns>
        T ConvertSendAndReceive<T>(D destination, object request, IDictionary<string, object> headers);

        /// <summary>
        /// Convert the given request object to serialized form, possibly using a message converter, send it as a message
        ///  to a default destination after applying the post processor, receive the reply and convert its body to the specified target type.
        /// </summary>
        /// <typeparam name="T">the target type of the reply</typeparam>
        /// <param name="request">payload for the request message to send</param>
        /// <param name="requestPostProcessor">the post processor to apply</param>
        /// <returns>the receieved message; or null</returns>
        T ConvertSendAndReceive<T>(object request, IMessagePostProcessor requestPostProcessor);

        /// <summary>
        /// Convert the given request object to serialized form, possibly using a message converter, send it as a message
        ///  to the specified destination after applying the post processor, receive the reply and convert its body to the specified target type.
        /// </summary>
        /// <typeparam name="T">the target type of the reply</typeparam>
        /// <param name="destination">the target destination</param>
        /// <param name="request">payload for the request message to send</param>
        /// <param name="requestPostProcessor">the post processor to apply</param>
        /// <returns>the receieved message; or null</returns>
        T ConvertSendAndReceive<T>(D destination, object request, IMessagePostProcessor requestPostProcessor);

        /// <summary>
        /// Convert the given request object to serialized form, possibly using a message converter, send it as a message
        ///  to the specified destination after applying the post processor, with the specified headers, receive the reply and convert its body to the specified target type.
        /// </summary>
        /// <typeparam name="T">the target type of the reply</typeparam>
        /// <param name="destination">the target destination</param>
        /// <param name="request">payload for the request message to send</param>
        /// <param name="headers">the headers to send</param>
        /// <param name="requestPostProcessor">the post processor to apply</param>
        /// <returns>the receieved message; or null</returns>
        T ConvertSendAndReceive<T>(D destination, object request, IDictionary<string, object> headers, IMessagePostProcessor requestPostProcessor);
    }
}
