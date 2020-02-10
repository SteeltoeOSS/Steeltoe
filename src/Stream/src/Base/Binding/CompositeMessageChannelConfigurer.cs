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
using Steeltoe.Stream.Binder;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Stream.Binding
{
    public class CompositeMessageChannelConfigurer : IMessageChannelAndSourceConfigurer
    {
        private readonly List<IMessageChannelConfigurer> _messageChannelConfigurers;

        public CompositeMessageChannelConfigurer(
                IEnumerable<IMessageChannelConfigurer> messageChannelConfigurers)
        {
            _messageChannelConfigurers = messageChannelConfigurers.ToList();
        }

        public void ConfigureInputChannel(IMessageChannel messageChannel, string channelName)
        {
            foreach (var messageChannelConfigurer in _messageChannelConfigurers)
            {
                messageChannelConfigurer.ConfigureInputChannel(messageChannel, channelName);
            }
        }

        public void ConfigureOutputChannel(IMessageChannel messageChannel, string channelName)
        {
            foreach (var messageChannelConfigurer in _messageChannelConfigurers)
            {
                messageChannelConfigurer.ConfigureOutputChannel(messageChannel, channelName);
            }
        }

        public void ConfigurePolledMessageSource(IPollableMessageSource binding, string name)
        {
            foreach (var cconfigurer in _messageChannelConfigurers)
            {
                if (cconfigurer is IMessageChannelAndSourceConfigurer)
                {
                    ((IMessageChannelAndSourceConfigurer)cconfigurer).ConfigurePolledMessageSource(binding, name);
                }
            }
        }
    }
}
