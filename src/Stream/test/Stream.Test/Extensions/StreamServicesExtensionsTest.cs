// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Expression.Internal;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Integration.Extensions;
using Steeltoe.Integration.Support.Converter;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Core;
using Steeltoe.Messaging.Handler.Attributes.Support;
using Steeltoe.Stream.Binding;
using Steeltoe.Stream.Configuration;
using Steeltoe.Stream.Messaging;
using Steeltoe.Stream.StreamHost;
using Xunit;

namespace Steeltoe.Stream.Extensions;

public class StreamServicesExtensionsTest
{
    [Fact]
    public void AddStreamConfiguration_AddsServices()
    {
        var container = new ServiceCollection();
        container.AddOptions();
        container.AddLogging(b => b.AddConsole());

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();
        container.AddStreamConfiguration(configurationRoot);
        ServiceProvider serviceProvider = container.BuildServiceProvider();
        ValidateConfigurationServices(serviceProvider);
    }

    [Fact]
    public void AddStreamCoreServices_AddsServices()
    {
        var container = new ServiceCollection();
        container.AddOptions();
        container.AddLogging(b => b.AddConsole());

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();
        container.AddSingleton<IConfiguration>(configurationRoot);
        container.AddCoreServices();
        container.AddIntegrationServices();
        container.AddBinderServices(configurationRoot);
        container.AddStreamCoreServices(configurationRoot);
        ServiceProvider serviceProvider = container.BuildServiceProvider();
        ValidateCoreServices(serviceProvider);
    }

    [Fact]
    public void AddStreamServices_AddsServices()
    {
        var container = new ServiceCollection();
        container.AddOptions();
        container.AddLogging(b => b.AddConsole());

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();
        container.AddSingleton<IConfiguration>(configurationRoot);
        container.AddStreamServices(configurationRoot);
        ServiceProvider serviceProvider = container.BuildServiceProvider();
        ValidateConfigurationServices(serviceProvider);
        ValidateCoreServices(serviceProvider);
    }

    [Fact]
    public void AddStreamsServicesGeneric_AddsServices()
    {
        var serviceCollection = new ServiceCollection();
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();

        serviceCollection.AddSingleton<IConfiguration>(configuration);
        serviceCollection.AddStreamServices<SampleSink>(configuration);

        ServiceProvider provider = serviceCollection.BuildServiceProvider();

        Assert.True(provider.GetService<SampleSink>() != null, "SampleSink not found in Container");

        Assert.True(provider.GetService<ISource>() != null, "ISource not found in Container");

        Assert.True(provider.GetService<ISink>() != null, "ISink not found in Container");

        Assert.True(provider.GetService<IExpressionParser>() != null, "IExpressionParser not found in Container");

        Assert.True(provider.GetService<IEvaluationContext>() != null, "IEvaluationContext not found in Container");
    }

    private void ValidateCoreServices(ServiceProvider serviceProvider)
    {
        Assert.NotNull(serviceProvider.GetService<IMessageConverterFactory>());
        Assert.NotNull(serviceProvider.GetService<ConfigurableCompositeMessageConverter>());
        Assert.NotNull(serviceProvider.GetService<ISmartMessageConverter>());
        Assert.NotNull(serviceProvider.GetService<IMessageHandlerMethodFactory>());
        Assert.NotNull(serviceProvider.GetService<IMessageChannelConfigurer>());
        Assert.NotNull(serviceProvider.GetService<CompositeMessageChannelConfigurer>());
        Assert.NotNull(serviceProvider.GetService<SubscribableChannelBindingTargetFactory>());
        Assert.NotNull(serviceProvider.GetService<MessageSourceBindingTargetFactory>());
        IEnumerable<IBindingTargetFactory> factories = serviceProvider.GetServices<IBindingTargetFactory>();
        Assert.Equal(2, factories.Count());
        Assert.NotNull(serviceProvider.GetService<BinderAwareChannelResolver>());
        Assert.NotNull(serviceProvider.GetService<IDestinationResolver<IMessageChannel>>());
        Assert.NotNull(serviceProvider.GetService<BindingService>());
        Assert.NotNull(serviceProvider.GetService<IBindingService>());
        Assert.NotNull(serviceProvider.GetService<DynamicDestinationsBindable>());
        Assert.NotNull(serviceProvider.GetService<IBindable>());
        Assert.NotNull(serviceProvider.GetService<MessageChannelStreamListenerResultAdapter>());
        Assert.NotNull(serviceProvider.GetService<IStreamListenerResultAdapter>());
        Assert.NotNull(serviceProvider.GetService<OutputBindingLifecycle>());
        Assert.NotNull(serviceProvider.GetService<InputBindingLifecycle>());
        IEnumerable<ILifecycle> lifecycles = serviceProvider.GetServices<ILifecycle>();
        Assert.Equal(3, lifecycles.Count());
        Assert.NotNull(serviceProvider.GetService<StreamListenerAttributeProcessor>());
    }

    private void ValidateConfigurationServices(ServiceProvider serviceProvider)
    {
        Assert.NotNull(serviceProvider.GetService<IOptionsMonitor<SpringIntegrationOptions>>());
        Assert.NotNull(serviceProvider.GetService<IOptionsMonitor<BindingServiceOptions>>());
    }
}
