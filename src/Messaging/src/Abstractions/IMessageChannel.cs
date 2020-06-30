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

using Steeltoe.Common.Services;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Messaging
{
    /// <summary>
    /// An abstraction that defines methods for sending messages;
    /// </summary>
    public interface IMessageChannel : IServiceNameAware
    {
        /// <summary>
        /// Send a message to this channel.
        /// </summary>
        /// <param name="message">the message to send</param>
        /// <param name="cancellationToken">token used to signal cancellation</param>
        /// <returns>a task to signal completion</returns>
        ValueTask<bool> SendAsync(IMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Send a message to this channel. If the message is sent successfuly,
        /// the method returns true. If the message cannot be sent due to a
        /// non-fatal reason, the method returns false. The method may also
        /// throw a Exception in case of non-recoverable errors.
        /// This method may block indefinitely, depending on the implementation.
        /// </summary>
        /// <param name="message">the message to send</param>
        /// <returns>true if the message is sent</returns>
        bool Send(IMessage message);

        /// <summary>
        ///  Send a message, blocking until either the message is accepted or the specified timeout period elapses.
        /// </summary>
        /// <param name="message">the message to send</param>
        /// <param name="timeout">the timeout in milliseconds; -1 for no timeout</param>
        /// <returns>true if the message is sent</returns>
        bool Send(IMessage message, int timeout);
    }
}
