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

namespace Steeltoe.Messaging
{
    /// <summary>
    /// A MessageChannel that maintains a registry of subscribers and invokes
    /// them to handle messages sent through this channel.
    /// </summary>
    public interface ISubscribableChannel : IMessageChannel
    {
        /// <summary>
        /// Register a message handler.
        /// </summary>
        /// <param name="handler">the handler to register</param>
        /// <returns>false if already registered; otherwise true</returns>
        bool Subscribe(IMessageHandler handler);

        /// <summary>
        /// Un-register a message handler.
        /// </summary>
        /// <param name="handler">the handler to remvoe</param>
        /// <returns>false if not registered; otherwise true</returns>
        bool Unsubscribe(IMessageHandler handler);
    }
}
