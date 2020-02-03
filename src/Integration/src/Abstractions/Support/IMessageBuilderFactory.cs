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

using Steeltoe.Messaging;

namespace Steeltoe.Integration.Support
{
    /// <summary>
    /// A factory for creating message builders
    /// </summary>
    public interface IMessageBuilderFactory
    {
        /// <summary>
        /// Create a message builder from the given message
        /// </summary>
        /// <typeparam name="T">the type of payload</typeparam>
        /// <param name="message">the message to use</param>
        /// <returns>the message builder</returns>
        IMessageBuilder<T> FromMessage<T>(IMessage<T> message);

        /// <summary>
        /// Create a message builder from the given message
        /// </summary>
        /// <param name="message">the message to use</param>
        /// <returns>the message builder</returns>
        IMessageBuilder FromMessage(IMessage message);

        /// <summary>
        /// Create a message builder from the given message payload
        /// </summary>
        /// <typeparam name="T">the type of the payload</typeparam>
        /// <param name="payload">the payload of the message</param>
        /// <returns>the message builder</returns>
        IMessageBuilder<T> WithPayload<T>(T payload);

        /// <summary>
        /// Create a message builder from the given message payload
        /// </summary>
        /// <param name="payload">the payload of the message</param>
        /// <returns>the message builder</returns>
        IMessageBuilder WithPayload(object payload);
    }
}
