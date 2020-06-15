// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
