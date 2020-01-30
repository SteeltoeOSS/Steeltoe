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

using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Core
{
    /// <summary>
    /// Operations for receiving messages from a destination.
    /// </summary>
    /// <typeparam name="D">the type of the destination</typeparam>
    public interface IMessageReceivingOperations<D>
    {
        /// <summary>
        /// Receive a message from a default destination
        /// </summary>
        /// <param name="cancellationToken">token used to signal cancelation</param>
        /// <returns>a task to signal completion</returns>
        Task<IMessage> ReceiveAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Receive a message from the given destination
        /// </summary>
        /// <param name="destination">the target destination</param>
        /// <param name="cancellationToken">token used to signal cancelation</param>
        /// <returns>a task to signal completion</returns>
        Task<IMessage> ReceiveAsync(D destination, CancellationToken cancellationToken = default);

        /// <summary>
        /// Receive a message from a default destination and convert its payload to the specified target type.
        /// </summary>
        /// <typeparam name="T">the type of the payload</typeparam>
        /// <param name="cancellationToken">token used to signal cancelation</param>
        /// <returns>a task to signal completion</returns>
        Task<T> ReceiveAndConvertAsync<T>(CancellationToken cancellationToken = default);

        /// <summary>
        /// Receive a message from the given destination and convert its payload to the specified target type.
        /// </summary>
        /// <typeparam name="T">the type of the payload</typeparam>
        /// <param name="destination">the target destination</param>
        /// <param name="cancellationToken">token used to signal cancelation</param>
        /// <returns>a task to signal completion</returns>
        Task<T> ReceiveAndConvertAsync<T>(D destination, CancellationToken cancellationToken = default);

        /// <summary>
        /// Receive a message from a default destination
        /// </summary>
        /// <returns>the received message; or null</returns>
        IMessage Receive();

        /// <summary>
        /// Receive a message from the given destination
        /// </summary>
        /// <param name="destination">the target destination</param>
        /// <returns>the received message; or null</returns>
        IMessage Receive(D destination);

        /// <summary>
        /// Receive a message from a default destination and convert its payload to the specified target type.
        /// </summary>
        /// <typeparam name="T">the type of the payload</typeparam>
        /// <returns>the received message; or null</returns>
        T ReceiveAndConvert<T>();

        /// <summary>
        /// Receive a message from the given destination and convert its payload to the specified target type.
        /// </summary>
        /// <typeparam name="T">the type of the payload</typeparam>
        /// <param name="destination">the target destination</param>
        /// <returns>the received message; or null</returns>
        T ReceiveAndConvert<T>(D destination);
    }
}
