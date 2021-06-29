// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
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
        private readonly Dictionary<string, ISubscribableChannel> _provisionedDestinations = new Dictionary<string, ISubscribableChannel>();
        private readonly IApplicationContext _context;

        public TestChannelBinderProvisioner(IApplicationContext context, InputDestination inputDestination, OutputDestination outputDestination)
        {
            InputDestination = inputDestination;
            OutputDestination = outputDestination;
            _context = context;
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
            _provisionedDestinations.TryGetValue(destinationName, out var destination);
            if (destination == null)
            {
                if (pubSub)
                {
                    destination = new PublishSubscribeChannel(_context);
                }
                else
                {
                    destination = new DirectChannel(_context);
                }

                ((AbstractMessageChannel)destination).ServiceName = destinationName;
                _provisionedDestinations.Add(destinationName, destination);
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
