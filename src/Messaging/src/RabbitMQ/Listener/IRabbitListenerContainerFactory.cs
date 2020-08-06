// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Services;

namespace Steeltoe.Messaging.RabbitMQ.Listener
{
    public interface IRabbitListenerContainerFactory : IServiceNameAware
    {
        IMessageListenerContainer CreateListenerContainer(IRabbitListenerEndpoint endpoint);

        IMessageListenerContainer CreateListenerContainer()
        {
            return CreateListenerContainer(null);
        }
    }

    public interface IRabbitListenerContainerFactory<out C> : IRabbitListenerContainerFactory
        where C : IMessageListenerContainer
    {
        new C CreateListenerContainer(IRabbitListenerEndpoint endpoint);

        new C CreateListenerContainer()
        {
            return CreateListenerContainer(null);
        }
    }
}
