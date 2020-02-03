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

namespace Steeltoe.Integration
{
    /// <summary>
    /// Base interface for any source of Messages that can be polled.
    /// </summary>
    public interface IMessageSource
    {
        /// <summary>
        /// Poll for a message from the source
        /// </summary>
        /// <returns>the message</returns>
        IMessage Receive();
    }

    /// <summary>
    /// A typed interface for any source of Messages that can be polled.
    /// </summary>
    /// <typeparam name="T">the type of payload in the message</typeparam>
    public interface IMessageSource<out T> : IMessage
    {
        /// <summary>
        /// Poll for a message from the source
        /// </summary>
        /// <returns>the message</returns>
        IMessage<T> Receive();
    }
}
