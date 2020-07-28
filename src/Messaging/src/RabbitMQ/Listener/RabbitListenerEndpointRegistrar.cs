// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Messaging.Handler.Attributes.Support;
using System;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Rabbit.Listener
{
    public class RabbitListenerEndpointRegistrar : IRabbitListenerEndpointRegistrar
    {
        public const string DEFAULT_SERVICE_NAME = nameof(RabbitListenerEndpointRegistrar);

        private readonly List<RabbitListenerEndpointDescriptor> _endpointDescriptors = new List<RabbitListenerEndpointDescriptor>();

        public RabbitListenerEndpointRegistrar(IMessageHandlerMethodFactory messageHandlerMethodFactory = null)
        {
            MessageHandlerMethodFactory = messageHandlerMethodFactory;
        }

        public IRabbitListenerEndpointRegistry EndpointRegistry { get; set; }

        public IMessageHandlerMethodFactory MessageHandlerMethodFactory { get; set; }

        public IRabbitListenerContainerFactory ContainerFactory { get; set; }

        public string ContainerFactoryServiceName { get; set; }

        public IApplicationContext ApplicationContext { get; set; }

        public bool StartImmediately { get; set; }

        public string ServiceName { get; set; } = DEFAULT_SERVICE_NAME;

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
            var descriptor = new RabbitListenerEndpointDescriptor(endpoint, factory);
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

        private IRabbitListenerContainerFactory ResolveContainerFactory(RabbitListenerEndpointDescriptor descriptor)
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

        private class RabbitListenerEndpointDescriptor
        {
            public RabbitListenerEndpointDescriptor(IRabbitListenerEndpoint endpoint, IRabbitListenerContainerFactory containerFactory)
            {
                Endpoint = endpoint;
                ContainerFactory = containerFactory;
            }

            public IRabbitListenerEndpoint Endpoint { get; }

            public IRabbitListenerContainerFactory ContainerFactory { get; }
        }
    }
}
