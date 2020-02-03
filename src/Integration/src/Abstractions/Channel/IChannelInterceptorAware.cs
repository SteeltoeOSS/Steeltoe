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

using Steeltoe.Messaging.Support;
using System.Collections.Generic;

namespace Steeltoe.Integration.Channel
{
    /// <summary>
    /// A marker interface providing the ability to configure ChannelInterceptors
    /// on MessageChannel implementations.
    /// </summary>
    public interface IChannelInterceptorAware
    {
        /// <summary>
        /// Gets or sets the channel interceptors
        /// </summary>
        List<IChannelInterceptor> ChannelInterceptors { get; set; }

        /// <summary>
        /// Add a interceptor to the channel
        /// </summary>
        /// <param name="interceptor">the interceptor</param>
        void AddInterceptor(IChannelInterceptor interceptor);

        /// <summary>
        /// Add an interceptor to the channel at the specified index
        /// </summary>
        /// <param name="index">the index to add the interceptor at</param>
        /// <param name="interceptor">the interceptor</param>
        void AddInterceptor(int index, IChannelInterceptor interceptor);

        /// <summary>
        /// Remove an intercetptor from the channel
        /// </summary>
        /// <param name="interceptor">the interceptor</param>
        /// <returns>succss or failure</returns>
        bool RemoveInterceptor(IChannelInterceptor interceptor);

        /// <summary>
        /// Remove an interceptor at the specified index
        /// </summary>
        /// <param name="index">the index</param>
        /// <returns>removed interceptor</returns>
        IChannelInterceptor RemoveInterceptor(int index);
    }
}
