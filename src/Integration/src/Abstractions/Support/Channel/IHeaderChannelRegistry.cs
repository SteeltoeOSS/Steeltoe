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
