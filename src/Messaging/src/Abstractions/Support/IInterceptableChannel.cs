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

using System.Collections.Generic;

namespace Steeltoe.Messaging.Support
{
    /// <summary>
    ///  A MessageChannel that maintains a list of ChannelInterceptors and allows interception of message sending.
    /// </summary>
    public interface IInterceptableChannel
    {
        /// <summary>
        /// Set the list of channel interceptors
        /// </summary>
        /// <param name="interceptors">the interceptors to use</param>
        void SetInterceptors(List<IChannelInterceptor> interceptors);

        /// <summary>
        /// Add an interceptor to the list
        /// </summary>
        /// <param name="interceptor">the interceptor to add</param>
        void AddInterceptor(IChannelInterceptor interceptor);

        /// <summary>
        /// Add an interceptor at the location specified by the index
        /// </summary>
        /// <param name="index">the index to add the interceptor at</param>
        /// <param name="interceptor">the interceptor to add</param>
        void AddInterceptor(int index, IChannelInterceptor interceptor);

        /// <summary>
        /// Get the interceptors for the channel
        /// </summary>
        /// <returns>the list of interceptors</returns>
        List<IChannelInterceptor> GetInterceptors();

        /// <summary>
        /// Remove the specified interceptor
        /// </summary>
        /// <param name="interceptor">the interceptor to remove</param>
        /// <returns>true if successful</returns>
        bool RemoveInterceptor(IChannelInterceptor interceptor);

        /// <summary>
        /// Remove the interceptor at the given index.
        /// </summary>
        /// <param name="index">the index to use</param>
        /// <returns>the interceptor removed</returns>
        IChannelInterceptor RemoveInterceptor(int index);
    }
}
