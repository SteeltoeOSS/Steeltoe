// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;
using Steeltoe.Stream.Binder;
using Steeltoe.Stream.Configuration;

namespace Steeltoe.Stream.Binding;

public class BinderAwareChannelResolver : DefaultMessageChannelDestinationResolver
{
    private readonly IBindingService _bindingService;
    private readonly SubscribableChannelBindingTargetFactory _bindingTargetFactory;
    private readonly DynamicDestinationsBindable _dynamicDestinationsBindable;
    private readonly INewDestinationBindingCallback _newBindingCallback;
    private readonly IOptionsMonitor<BindingServiceOptions> _optionsMonitor;

    public BindingServiceOptions Options => _optionsMonitor.CurrentValue;

    public BinderAwareChannelResolver(IApplicationContext context, IOptionsMonitor<BindingServiceOptions> optionsMonitor, IBindingService bindingService,
        SubscribableChannelBindingTargetFactory bindingTargetFactory, DynamicDestinationsBindable dynamicDestinationsBindable)
        : this(context, optionsMonitor, bindingService, bindingTargetFactory, dynamicDestinationsBindable, null)
    {
    }

    public BinderAwareChannelResolver(IApplicationContext context, IOptionsMonitor<BindingServiceOptions> optionsMonitor, IBindingService bindingService,
        SubscribableChannelBindingTargetFactory bindingTargetFactory, DynamicDestinationsBindable dynamicDestinationsBindable,
        INewDestinationBindingCallback callback)
        : base(context)
    {
        ArgumentGuard.NotNull(bindingService);
        ArgumentGuard.NotNull(bindingTargetFactory);

        _dynamicDestinationsBindable = dynamicDestinationsBindable;
        _optionsMonitor = optionsMonitor;
        _bindingService = bindingService;
        _bindingTargetFactory = bindingTargetFactory;
        _newBindingCallback = callback;
    }

    public override IMessageChannel ResolveDestination(string name)
    {
        BindingServiceOptions options = Options;
        List<string> dynamicDestinations = options.DynamicDestinations;

        IMessageChannel channel;
        bool dynamicAllowed = dynamicDestinations.Count == 0 || dynamicDestinations.Contains(name);

        try
        {
            channel = base.ResolveDestination(name);

            if (channel == null && dynamicAllowed)
            {
                channel = CreateDynamic(name, options);
            }
        }
        catch (DestinationResolutionException)
        {
            if (!dynamicAllowed)
            {
                throw;
            }

            channel = CreateDynamic(name, options);
        }

        return channel;
    }

    private IMessageChannel CreateDynamic(string name, BindingServiceOptions options)
    {
        ISubscribableChannel channel = _bindingTargetFactory.CreateOutput(name);

        if (_newBindingCallback != null)
        {
            ProducerOptions producerOptions = options.GetProducerOptions(name);

            _newBindingCallback.Configure(name, channel, producerOptions, null);
            options.UpdateProducerOptions(name, producerOptions);
        }

        IBinding binding = _bindingService.BindProducer(channel, name);
        _dynamicDestinationsBindable.AddOutputBinding(name, binding);
        return channel;
    }

    public interface INewDestinationBindingCallback
    {
        void Configure(string channelName, IMessageChannel channel, ProducerOptions producerOptions, object extendedProducerOptions);
    }
}
