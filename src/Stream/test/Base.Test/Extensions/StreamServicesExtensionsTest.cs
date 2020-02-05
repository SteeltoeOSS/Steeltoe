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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Integration.Support.Converter;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Core;
using Steeltoe.Messaging.Handler.Attributes.Support;
using Steeltoe.Stream.Binding;
using Steeltoe.Stream.Config;
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
            container.AddCoreServices();
            container.AddIntegrationServices(config);
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
            container.AddStreamServices(config);
            var serviceProvider = container.BuildServiceProvider();
            ValidateConfigurationServices(serviceProvider);
            ValidateCoreServices(serviceProvider);
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
