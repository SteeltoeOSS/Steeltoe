// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Integration.Extensions;
using Steeltoe.Integration.Support.Converter;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Core;
using Steeltoe.Messaging.Handler.Attributes.Support;
using Steeltoe.Stream.Binding;
using Steeltoe.Stream.Config;
using Steeltoe.Stream.StreamsHost;
using System.Linq;
using Xunit;

namespace Steeltoe.Stream.Extensions
{
    public class StreamServicesExtensionsTest
    {
        [Fact]
        public void AddStreamConfiguration_AddsServices()
        {
            var container = new ServiceCollection();
            container.AddOptions();
            container.AddLogging((b) => b.AddConsole());

            var config = new ConfigurationBuilder().Build();
            container.AddStreamConfiguration(config);
            var serviceProvider = container.BuildServiceProvider();
            ValidateConfigurationServices(serviceProvider);
        }

        [Fact]
        public void AddStreamCoreServices_AddsServices()
        {
            var container = new ServiceCollection();
            container.AddOptions();
            container.AddLogging((b) => b.AddConsole());

            var config = new ConfigurationBuilder().Build();
            container.AddSingleton<IConfiguration>(config);
            container.AddCoreServices();
            container.AddIntegrationServices();
            container.AddBinderServices(config);
            container.AddStreamCoreServices(config);
            var serviceProvider = container.BuildServiceProvider();
            ValidateCoreServices(serviceProvider);
        }

        [Fact]
        public void AddStreamServices_AddsServices()
        {
            var container = new ServiceCollection();
            container.AddOptions();
            container.AddLogging((b) => b.AddConsole());

            var config = new ConfigurationBuilder().Build();
            container.AddSingleton<IConfiguration>(config);
            container.AddStreamServices(config);
            var serviceProvider = container.BuildServiceProvider();
            ValidateConfigurationServices(serviceProvider);
            ValidateCoreServices(serviceProvider);
        }

        [Fact]
        public void AddStreamsServicesGeneric_AddsServices()
        {
            var serviceCollection = new ServiceCollection();
            var configuration = new ConfigurationBuilder().Build();
            serviceCollection.AddStreamServices<SampleSink>(configuration);

            var service = serviceCollection.BuildServiceProvider().GetService<SampleSink>();
            Assert.NotNull(service);
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
            var factories = serviceProvider.GetServices<IBindingTargetFactory>();
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
            var lifes = serviceProvider.GetServices<ILifecycle>();
            Assert.Equal(3, lifes.Count());
            Assert.NotNull(serviceProvider.GetService<StreamListenerAttributeProcessor>());
        }

        private void ValidateConfigurationServices(ServiceProvider serviceProvider)
        {
            Assert.NotNull(serviceProvider.GetService<IOptionsMonitor<SpringIntegrationOptions>>());
            Assert.NotNull(serviceProvider.GetService<IOptionsMonitor<BindingServiceOptions>>());
        }
    }
}
