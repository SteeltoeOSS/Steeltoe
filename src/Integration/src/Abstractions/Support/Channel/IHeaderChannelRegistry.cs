// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;

namespace Steeltoe.Integration.Support.Channel
{
    /// <summary>
    /// Implementations convert a channel to a name, retaining a reference to the channel keyed by the name.
    /// </summary>
    public interface IHeaderChannelRegistry
    {
        /// <summary>
        ///  Converts the channel to a name (String). If the channel is not a MessageChannel, it is returned unchanged.
        /// </summary>
        /// <param name="channel">the channel</param>
        /// <returns>the channel name or the channel if not a MessageChannel</returns>
        object ChannelToChannelName(object channel);

        /// <summary>
        ///  Converts the channel to a name (String). If the channel is not a MessageChannel, it is returned unchanged.
        /// </summary>
        /// <param name="channel">the channel</param>
        /// <param name="timeToLive">how long in milliseconds the channel mapping should remain in registry</param>
        /// <returns>the channel name or the channel if not a MessageChannel</returns>
        object ChannelToChannelName(object channel, long timeToLive);

        /// <summary>
        /// Converts the channel name back to a MessageChannel (if it is registered)
        /// </summary>
        /// <param name="name">the channel name</param>
        /// <returns>The channel, or null if there is no channel registered with the name.</returns>
        IMessageChannel ChannelNameToChannel(string name);
    }
}
