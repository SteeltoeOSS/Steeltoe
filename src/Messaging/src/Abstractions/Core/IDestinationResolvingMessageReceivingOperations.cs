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
    /// Provides operations for receiving messages from a destination specifed as a resolvable string
    /// </summary>
    /// <typeparam name="D">the destination type</typeparam>
    public interface IDestinationResolvingMessageReceivingOperations<D> : IMessageReceivingOperations<D>
    {
        /// <summary>
        /// Resolve the given destination and receive a message from it
        /// </summary>
        /// <param name="destinationName">the destination name to resolve</param>
        /// <param name="cancellationToken">a token used to cancel the operation</param>
        /// <returns>a task to signal completion</returns>
        Task<IMessage> ReceiveAsync(string destinationName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolve the given destination, receive a message from it, convert the payload to the specified target type
        /// </summary>
        /// <typeparam name="T">the target type</typeparam>
        /// <param name="destinationName">the destination name to resolve</param>
        /// <param name="cancellationToken">a token used to cancel the operation</param>
        /// <returns>a task to signal completion</returns>
        Task<T> ReceiveAndConvertAsync<T>(string destinationName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolve the given destination and receive a message from it
        /// </summary>
        /// <param name="destinationName">the destination name to resolve</param>
        /// <returns>the received message</returns>
        IMessage Receive(string destinationName);

        /// <summary>
        /// Resolve the given destination, receive a message from it, convert the payload to the specified target type
        /// </summary>
        /// <typeparam name="T">the target type</typeparam>
        /// <param name="destinationName">the destination name to resolve</param>
        /// <returns>the received message</returns>
        T ReceiveAndConvert<T>(string destinationName);
    }
}
