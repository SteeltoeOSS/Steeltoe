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

using Steeltoe.Common.Contexts;
using Steeltoe.Messaging.Handler.Attributes.Support;
using System;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Rabbit.Listener
{
    public class RabbitListenerEndpointRegistrar
    {
        private readonly List<AmqpListenerEndpointDescriptor> _endpointDescriptors = new List<AmqpListenerEndpointDescriptor>();

        public RabbitListenerEndpointRegistry EndpointRegistry { get; set; }

        public IMessageHandlerMethodFactory MessageHandlerMethodFactory { get; set; }

        public IRabbitListenerContainerFactory ContainerFactory { get; set; }

        public string ContainerFactoryServiceName { get; set; }

        public IApplicationContext ApplicationContext { get; set; }

        public bool StartImmediately { get; set; }

        public void Initialize()
        {
            RegisterAllEndpoints();
        }

        public void RegisterEndpoint(IRabbitListenerEndpoint endpoint, IRabbitListenerContainerFactory factory)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            if (string.IsNullOrEmpty(endpoint.Id))
            {
                throw new InvalidOperationException("Endpoint id must be set");
            }

            if (StartImmediately && EndpointRegistry == null)
            {
                throw new InvalidOperationException("No registry available");
            }

            // Factory may be null, we defer the resolution right before actually creating the container
            var descriptor = new AmqpListenerEndpointDescriptor(endpoint, factory);
            lock (_endpointDescriptors)
            {
                if (StartImmediately)
                { // Register and start immediately
                    EndpointRegistry.RegisterListenerContainer(descriptor.Endpoint, ResolveContainerFactory(descriptor), true);
                }
                else
                {
                    _endpointDescriptors.Add(descriptor);
                }
            }
        }

        public void RegisterEndpoint(IRabbitListenerEndpoint endpoint)
        {
            RegisterEndpoint(endpoint, null);
        }

        protected void RegisterAllEndpoints()
        {
            if (EndpointRegistry == null)
            {
                throw new InvalidOperationException("No registry available");
            }

            lock (_endpointDescriptors)
            {
                foreach (var descriptor in _endpointDescriptors)
                {
                    EndpointRegistry.RegisterListenerContainer(descriptor.Endpoint, ResolveContainerFactory(descriptor));
                }

                StartImmediately = true;  // trigger immediate startup
            }
        }

        private IRabbitListenerContainerFactory ResolveContainerFactory(AmqpListenerEndpointDescriptor descriptor)
        {
            if (descriptor.ContainerFactory != null)
            {
                return descriptor.ContainerFactory;
            }
            else if (ContainerFactory != null)
            {
                return ContainerFactory;
            }
            else if (ContainerFactoryServiceName != null)
            {
                if (ApplicationContext == null)
                {
                    throw new InvalidOperationException("BeanFactory must be set to obtain container factory by bean name");
                }

                ContainerFactory = ApplicationContext.GetService<IRabbitListenerContainerFactory>(ContainerFactoryServiceName);
                return ContainerFactory;
            }
            else
            {
                throw new InvalidOperationException("Could not resolve the IRabbitListenerContainerFactory to use for [" + descriptor.Endpoint + "] no factory was given and no default is set.");
            }
        }

        private class AmqpListenerEndpointDescriptor
        {
            public AmqpListenerEndpointDescriptor(IRabbitListenerEndpoint endpoint, IRabbitListenerContainerFactory containerFactory)
            {
                Endpoint = endpoint;
                ContainerFactory = containerFactory;
            }

            public IRabbitListenerEndpoint Endpoint { get; }

            public IRabbitListenerContainerFactory ContainerFactory { get; }
        }
    }
}
