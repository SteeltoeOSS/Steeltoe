// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Channel;
using Steeltoe.Messaging;
using Steeltoe.Stream.Config;
using Steeltoe.Stream.Provisioning;

namespace Steeltoe.Stream.TestBinder;

public class TestChannelBinderProvisioner : IProvisioningProvider
{
    private readonly Dictionary<string, ISubscribableChannel> _provisionedDestinations = new();
    private readonly IApplicationContext _context;

    public InputDestination InputDestination { get; }

    public OutputDestination OutputDestination { get; }

    public TestChannelBinderProvisioner(IApplicationContext context, InputDestination inputDestination, OutputDestination outputDestination)
    {
        InputDestination = inputDestination;
        OutputDestination = outputDestination;
        _context = context;
    }

    public IProducerDestination ProvisionProducerDestination(string name, IProducerOptions options)
    {
        ISubscribableChannel destination = ProvisionDestination(name, true);
        OutputDestination.Channel = destination;
        return new SpringIntegrationProducerDestination(name, destination);
    }

    public IConsumerDestination ProvisionConsumerDestination(string name, string group, IConsumerOptions options)
    {
        ISubscribableChannel destination = ProvisionDestination(name, false);

        if (InputDestination != null)
        {
            InputDestination.Channel = destination;
        }

        return new SpringIntegrationConsumerDestination(name, destination);
    }

    private ISubscribableChannel ProvisionDestination(string name, bool pubSub)
    {
        string destinationName = $"{name}.destination";
        _provisionedDestinations.TryGetValue(destinationName, out ISubscribableChannel destination);

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

    internal sealed class SpringIntegrationConsumerDestination : IConsumerDestination
    {
        public ISubscribableChannel Channel { get; }

        public string Name { get; }

        public SpringIntegrationConsumerDestination(string name, ISubscribableChannel channel)
        {
            Name = name;
            Channel = channel;
        }

        public string GetNameForPartition(int partition)
        {
            return Name + partition;
        }
    }

    internal sealed class SpringIntegrationProducerDestination : IProducerDestination
    {
        public ISubscribableChannel Channel { get; }

        public string Name { get; }

        public SpringIntegrationProducerDestination(string name, ISubscribableChannel channel)
        {
            Name = name;
            Channel = channel;
        }

        public string GetNameForPartition(int partition)
        {
            return Name + partition;
        }
    }
}
