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

using Steeltoe.Integration.Channel;
using Steeltoe.Messaging;
using Steeltoe.Stream.Config;
using Steeltoe.Stream.Provisioning;
using System;
using System.Collections.Generic;

namespace Steeltoe.Stream.TestBinder
{
    public class TestChannelBinderProvisioner : IProvisioningProvider
    {
        private readonly Dictionary<string, ISubscribableChannel> provisionedDestinations = new Dictionary<string, ISubscribableChannel>();
        private readonly IServiceProvider serviceProvider;

        public TestChannelBinderProvisioner(IServiceProvider serviceProvider, InputDestination inputDestination, OutputDestination outputDestination)
        {
            InputDestination = inputDestination;
            OutputDestination = outputDestination;
            this.serviceProvider = serviceProvider;
        }

        public InputDestination InputDestination { get; }

        public OutputDestination OutputDestination { get; }

        public IProducerDestination ProvisionProducerDestination(string name, IProducerOptions options)
        {
            var destination = ProvisionDestination(name, true);
            OutputDestination.Channel = destination;
            return new SpringIntegrationProducerDestination(name, destination);
        }

        public IConsumerDestination ProvisionConsumerDestination(string name, string group, IConsumerOptions properties)
        {
            var destination = ProvisionDestination(name, false);
            if (InputDestination != null)
            {
                InputDestination.Channel = destination;
            }

            return new SpringIntegrationConsumerDestination(name, destination);
        }

        private ISubscribableChannel ProvisionDestination(string name, bool pubSub)
        {
            var destinationName = name + ".destination";
            provisionedDestinations.TryGetValue(destinationName, out var destination);
            if (destination == null)
            {
                if (pubSub)
                {
                    destination = new PublishSubscribeChannel(serviceProvider);
                }
                else
                {
                    destination = new DirectChannel(serviceProvider);
                }

                ((AbstractMessageChannel)destination).Name = destinationName;
                provisionedDestinations.Add(destinationName, destination);
            }

            return destination;
        }

        internal class SpringIntegrationConsumerDestination : IConsumerDestination
        {
            public SpringIntegrationConsumerDestination(string name, ISubscribableChannel channel)
            {
                Name = name;
                Channel = channel;
            }

            public string GetNameForPartition(int partition)
            {
                return Name + partition;
            }

            public ISubscribableChannel Channel { get; }

            public string Name { get; }
        }

        internal class SpringIntegrationProducerDestination : IProducerDestination
        {
            public SpringIntegrationProducerDestination(string name, ISubscribableChannel channel)
            {
                Name = name;
                Channel = channel;
            }

            public string GetNameForPartition(int partition)
            {
                return Name + partition;
            }

            public ISubscribableChannel Channel { get; }

            public string Name { get; }
        }
    }
}
