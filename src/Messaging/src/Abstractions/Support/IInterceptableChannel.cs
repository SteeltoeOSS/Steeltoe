// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
