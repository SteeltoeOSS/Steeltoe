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
    /// Operations for sending messages to a destination.
    /// </summary>
    /// <typeparam name="D">the type of the destination</typeparam>
    public interface IMessageSendingOperations<D>
    {
        /// <summary>
        /// Send a message to a default destination.
        /// </summary>
        /// <param name="message">the message to send</param>
        /// <param name="cancellationToken">token used to signal cancelation</param>
        /// <returns>a task to signal completion</returns>
        Task SendAsync(IMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Send a message to the given destination.
        /// </summary>
        /// <param name="destination">the target destination</param>
        /// <param name="message">the message to send</param>
        /// <param name="cancellationToken">token used to signal cancelation</param>
        /// <returns>a task to signal completion</returns>
        Task SendAsync(D destination, IMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Convert the given object to serialized form, possibly using a message converter, wrap it as a message
        /// and send it to a default destination.
        /// </summary>
        /// <param name="payload">the payload to send</param>
        /// <param name="cancellationToken">token used to signal cancelation</param>
        /// <returns>a task to signal completion</returns>
        Task ConvertAndSendAsync(object payload, CancellationToken cancellationToken = default);

        /// <summary>
        /// Convert the given object to serialized form, possibly using a message converter, wrap it as a message
        /// and send it to a specified destination.
        /// </summary>
        /// <param name="destination">the target destination</param>
        /// <param name="payload">the payload to send</param>
        /// <param name="cancellationToken">token used to signal cancelation</param>
        /// <returns>a task to signal completion</returns>
        Task ConvertAndSendAsync(D destination, object payload, CancellationToken cancellationToken = default);

        /// <summary>
        /// Convert the given object to serialized form, possibly using a message converter, wrap it as a message
        /// with the provided headers, and send it to a specified destination.
        /// </summary>
        /// <param name="destination">the target destination</param>
        /// <param name="payload">the payload to send</param>
        /// <param name="headers">the headers to send</param>
        /// <param name="cancellationToken">token used to signal cancelation</param>
        /// <returns>a task to signal completion</returns>
        Task ConvertAndSendAsync(D destination, object payload, IDictionary<string, object> headers, CancellationToken cancellationToken = default);

        /// <summary>
        /// Convert the given object to serialized form, possibly using a message converter, wrap it as a message,
        /// apply the psot processor, and send it to the default destination.
        /// </summary>
        /// <param name="payload">the payload to send</param>
        /// <param name="postProcessor">the post processor to apply</param>
        /// <param name="cancellationToken">token used to signal cancelation</param>
        /// <returns>a task to signal completion</returns>
        Task ConvertAndSendAsync(object payload, IMessagePostProcessor postProcessor, CancellationToken cancellationToken = default);

        /// <summary>
        /// Convert the given object to serialized form, possibly using a message converter, wrap it as a message,
        /// apply the psot processor, and send it to the specified destination.
        /// </summary>
        /// <param name="destination">the target destination</param>
        /// <param name="payload">the payload to send</param>
        /// <param name="postProcessor">the post processor to apply</param>
        /// <param name="cancellationToken">token used to signal cancelation</param>
        /// <returns>a task to signal completion</returns>
        Task ConvertAndSendAsync(D destination, object payload, IMessagePostProcessor postProcessor, CancellationToken cancellationToken = default);

        /// <summary>
        /// Convert the given object to serialized form, possibly using a message converter, wrap it as a message,
        /// with the provided headers, apply the psot processor, and send it to the specified destination.
        /// </summary>
        /// <param name="destination">the target destination</param>
        /// <param name="payload">the payload to send</param>
        /// <param name="headers">the headers to send</param>
        /// <param name="postProcessor">the post processor to apply</param>
        /// <param name="cancellationToken">token used to signal cancelation</param>
        /// <returns>a task to signal completion</returns>
        Task ConvertAndSendAsync(D destination, object payload, IDictionary<string, object> headers, IMessagePostProcessor postProcessor, CancellationToken cancellationToken = default);

        /// <summary>
        /// Send a message to a default destination.
        /// </summary>
        /// <param name="message">the message to send</param>
        void Send(IMessage message);

        /// <summary>
        /// Send a message to the given destination.
        /// </summary>
        /// <param name="destination">the target destination</param>
        /// <param name="message">the message to send</param>
        void Send(D destination, IMessage message);

        /// <summary>
        /// Convert the given object to serialized form, possibly using a message converter, wrap it as a message
        /// and send it to a default destination.
        /// </summary>
        /// <param name="payload">the payload to send</param>
        void ConvertAndSend(object payload);

        /// <summary>
        /// Convert the given object to serialized form, possibly using a message converter, wrap it as a message
        /// and send it to a specified destination.
        /// </summary>
        /// <param name="destination">the target destination</param>
        /// <param name="payload">the payload to send</param>
        void ConvertAndSend(D destination, object payload);

        /// <summary>
        /// Convert the given object to serialized form, possibly using a message converter, wrap it as a message
        /// with the provided headers, and send it to a specified destination.
        /// </summary>
        /// <param name="destination">the target destination</param>
        /// <param name="payload">the payload to send</param>
        /// <param name="headers">the headers to send</param>
        void ConvertAndSend(D destination, object payload, IDictionary<string, object> headers);

        /// <summary>
        /// Convert the given object to serialized form, possibly using a message converter, wrap it as a message,
        /// apply the psot processor, and send it to the default destination.
        /// </summary>
        /// <param name="payload">the payload to send</param>
        /// <param name="postProcessor">the post processor to apply</param>
        void ConvertAndSend(object payload, IMessagePostProcessor postProcessor);

        /// <summary>
        /// Convert the given object to serialized form, possibly using a message converter, wrap it as a message,
        /// apply the psot processor, and send it to the specified destination.
        /// </summary>
        /// <param name="destination">the target destination</param>
        /// <param name="payload">the payload to send</param>
        /// <param name="postProcessor">the post processor to apply</param>
        void ConvertAndSend(D destination, object payload, IMessagePostProcessor postProcessor);

        /// <summary>
        /// Convert the given object to serialized form, possibly using a message converter, wrap it as a message,
        /// with the provided headers, apply the psot processor, and send it to the specified destination.
        /// </summary>
        /// <param name="destination">the target destination</param>
        /// <param name="payload">the payload to send</param>
        /// <param name="headers">the headers to send</param>
        /// <param name="postProcessor">the post processor to apply</param>
        void ConvertAndSend(D destination, object payload, IDictionary<string, object> headers, IMessagePostProcessor postProcessor);
    }
}
