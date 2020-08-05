// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;

namespace Steeltoe.Stream.Binding
{
    public interface IMessageChannelConfigurer
    {
        void ConfigureInputChannel(IMessageChannel messageChannel, string channelName);

        void ConfigureOutputChannel(IMessageChannel messageChannel, string channelName);
    }
}
