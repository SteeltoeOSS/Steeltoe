// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Common.Services;
using Steeltoe.Messaging.Handler.Attributes.Support;

namespace Steeltoe.Messaging.RabbitMQ.Listener;

public interface IRabbitListenerEndpointRegistrar : IServiceNameAware
{
    IRabbitListenerEndpointRegistry EndpointRegistry { get; set; }

    IMessageHandlerMethodFactory MessageHandlerMethodFactory { get; set; }

    IRabbitListenerContainerFactory ContainerFactory { get; set; }

    IApplicationContext ApplicationContext { get; set; }

    string ContainerFactoryServiceName { get; set; }

    bool StartImmediately { get; set; }

    void Initialize();

    void RegisterEndpoint(IRabbitListenerEndpoint endpoint, IRabbitListenerContainerFactory factory);

    void RegisterEndpoint(IRabbitListenerEndpoint endpoint);
}
