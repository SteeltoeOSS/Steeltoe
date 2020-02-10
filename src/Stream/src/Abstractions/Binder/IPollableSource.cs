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

namespace Steeltoe.Stream.Binder
{
    /// <summary>
    /// An abstraction which defines a mechanism to poll a consumer
    /// </summary>
    public interface IPollableSource
    {
        /// <summary>
        /// Poll the consumer
        /// </summary>
        /// <param name="handler">the handler to process message</param>
        /// <returns>true if a message was handled</returns>
        bool Poll(object handler);

        /// <summary>
        /// Poll the consumer and convert the payload to the specified type. Throw a
        /// RequeueCurrentMessageException to force the current message to be requeued
        /// in the broker(after retries are exhausted, if configured).
        /// </summary>
        /// <param name="handler">the handler</param>
        /// <param name="type">the type of the payload</param>
        /// <returns>true if a message was handled</returns>
        bool Poll(object handler, Type type);
    }

    /// <summary>
    /// An abstraction which defines a mechanism to poll a consumer
    /// </summary>
    /// <typeparam name="H">the handler type</typeparam>
    public interface IPollableSource<in H> : IPollableSource
    {
        /// <summary>
        /// Poll the consumer
        /// </summary>
        /// <param name="handler">the handler to process message</param>
        /// <returns>true if a message was handled</returns>
        bool Poll(H handler);

        /// <summary>
        /// Poll the consumer and convert the payload to the specified type. Throw a
        /// RequeueCurrentMessageException to force the current message to be requeued
        /// in the broker(after retries are exhausted, if configured).
        /// </summary>
        /// <param name="handler">the handler</param>
        /// <param name="type">the type of the payload</param>
        /// <returns>true if a message was handled</returns>
        bool Poll(H handler, Type type);
    }
}
