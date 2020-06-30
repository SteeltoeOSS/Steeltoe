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

using System;
using System.Collections.Generic;

namespace Steeltoe.Messaging
{
    /// <summary>
    /// The headers for a message
    /// </summary>
    public interface IMessageHeaders : IDictionary<string, object>
    {
        /// <summary>
        /// Gets a header value given its key
        /// </summary>
        /// <typeparam name="T">the type of the value returned</typeparam>
        /// <param name="key">the name of the header</param>
        /// <returns>the value or null if not found</returns>
        T Get<T>(string key);

        /// <summary>
        /// Gets the ID header value
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the timestamp header value
        /// </summary>
        long? Timestamp { get; }

        /// <summary>
        /// Gets the reply channel the message is for
        /// </summary>
        object ReplyChannel { get; }

        /// <summary>
        /// Gets the error channel the message is for
        /// </summary>
        object ErrorChannel { get; }
    }
}
