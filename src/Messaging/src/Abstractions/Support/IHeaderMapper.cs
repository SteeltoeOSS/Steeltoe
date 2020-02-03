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

namespace Steeltoe.Messaging.Support
{
    /// <summary>
    /// Generic strategy interface for mapping MessageHeaders to and from other types of objects.
    /// </summary>
    /// <typeparam name="T">type of the instance to and from which headers will be mapped</typeparam>
    public interface IHeaderMapper<in T>
    {
        /// <summary>
        /// Map from the given MessageHeaders to the specified target message.
        /// </summary>
        /// <param name="headers">the incoming message headers</param>
        /// <param name="target">the native target message</param>
        void FromHeaders(IMessageHeaders headers, T target);

        /// <summary>
        /// Map from the given target message to abstracted MessageHeaders.
        /// </summary>
        /// <param name="source">the native target message</param>
        /// <returns>the mapped message headers</returns>
        IMessageHeaders ToHeaders(T source);
    }
}
