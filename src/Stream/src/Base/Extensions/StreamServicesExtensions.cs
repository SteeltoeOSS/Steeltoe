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
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Integration.Support.Converter;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Core;
using Steeltoe.Messaging.Handler.Attributes.Support;
using Steeltoe.Stream.Binding;
using Steeltoe.Stream.Config;
using Steeltoe.Stream.Converter;

namespace Steeltoe.Stream.Extensions
{
    public static class StreamServicesExtensions
    {
        public static void AddStreamConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<SpringIntegrationOptions>(configuration.GetSection(SpringIntegrationOptions.PREFIX));
            services.Configure<BindingServiceOptions>(configuration.GetSection(BindingServiceOptions.PREFIX));

            services.PostConfigure<BindingServiceOptions>(o => o.PostProcess());
        }

        public static void AddStreamCoreServices(this IServiceCollection services, IConfiguration configuration)
        {
            // ContentTypeConfiguration
            services.TryAddSingleton<IMessageConverterFactory, CompositeMessageConverterFactory>();
            services.TryAddSingleton<ConfigurableCompositeMessageConverter>();
            services.AddSingleton<ISmartMessageConverter>((p) => p.GetRequiredService<ConfigurableCompositeMessageConverter>());

            // SpelExpressionConverterConfiguration ?

            // messageHandlerFactoryMethod
            services.TryAddSingleton<IMessageHandlerMethodFactory, StreamMessageHandlerMethodFactory>();

            // messageConverterConfigurer
            services.TryAddSingleton<MessageConverterConfigurer>();
            services.TryAddSingleton<IMessageChannelConfigurer>((p) => p.GetRequiredService<MessageConverterConfigurer>());

            // compositeMessageChannelConfigurer
            services.TryAddSingleton<CompositeMessageChannelConfigurer>();

            // channelFactory
            services.TryAddSingleton<SubscribableChannelBindingTargetFactory>();
            services.AddSingleton<IBindingTargetFactory>((p) => p.GetRequiredService<SubscribableChannelBindingTargetFactory>());

            // messageSourceFactory
            services.TryAddSingleton<MessageSourceBindingTargetFactory>();
            services.AddSingleton<IBindingTargetFactory>((p) => p.GetRequiredService<MessageSourceBindingTargetFactory>());

            // binderAwareChannelResolver
            services.TryAddSingleton<BinderAwareChannelResolver>();
            services.TryAddSingleton<IDestinationResolver<IMessageChannel>>((p) => p.GetRequiredService<BinderAwareChannelResolver>());

            // bindingService
            services.TryAddSingleton<BindingService>();
            services.TryAddSingleton<IBindingService>((p) => p.GetRequiredService<BindingService>());

            // dynamicDestinationsBindable
            services.TryAddSingleton<DynamicDestinationsBindable>();
            services.AddSingleton<IBindable>((p) => p.GetRequiredService<DynamicDestinationsBindable>());

            // spelPropertyAccessorRegistrar

            // messageChannelStreamListenerResultAdapter
            services.TryAddSingleton<MessageChannelStreamListenerResultAdapter>();
            services.AddSingleton<IStreamListenerResultAdapter>((p) => p.GetRequiredService<MessageChannelStreamListenerResultAdapter>());

            // outputBindingLifecycle
            services.TryAddSingleton<OutputBindingLifecycle>();
            services.AddSingleton<ILifecycle>((p) => p.GetRequiredService<OutputBindingLifecycle>());

            // inputBindingLifecycle
            services.TryAddSingleton<InputBindingLifecycle>();
            services.AddSingleton<ILifecycle>((p) => p.GetRequiredService<InputBindingLifecycle>());

            // contextStartAfterRefreshListener

            // binderAwareRouterBeanPostProcessor

            // appListener (TODO: This addNotPropagatedHeaders to AbstractReplyProducingMessageHandler(s) on context refresh

            // defaultPoller in ChannelBindingAutoConfiguration ? (PollerMetadata) // DefaultPollerProperties

            // streamListenerAnnotationBeanPostProcessor
            services.TryAddSingleton<StreamListenerAttributeProcessor>();
        }

        public static void AddStreamServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddStreamConfiguration(configuration);

            services.AddCoreServices();

            services.AddIntegrationServices(configuration);

            services.AddBinderServices(configuration);

            services.AddStreamCoreServices(configuration);
        }
    }
}
