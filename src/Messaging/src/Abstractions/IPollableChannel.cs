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

namespace Steeltoe.Messaging
{
    /// <summary>
    /// A MessageChannel from which messages may be actively received through polling
    /// </summary>
    public interface IPollableChannel : IMessageChannel
    {
        /// <summary>
        /// Receive a message from this channel
        /// </summary>
        /// <param name="cancellationToken">token used to signal cancelation</param>
        /// <returns>a task to signal completion</returns>
        ValueTask<IMessage> ReceiveAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Receive a message from this channel, blocking forever if necessary.
        /// </summary>
        /// <returns>the message</returns>
        IMessage Receive();

        /// <summary>
        /// Receive a message from this channel, blocking until either a message is available
        /// or the specified timeout period elapses.
        /// </summary>
        /// <param name="timeout">the timeout value in milliseconds</param>
        /// <returns>the message or null</returns>
        IMessage Receive(int timeout);
    }
}
