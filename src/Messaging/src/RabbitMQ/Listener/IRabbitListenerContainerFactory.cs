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

using Steeltoe.Common.Services;

namespace Steeltoe.Messaging.Rabbit.Listener
{
    public interface IRabbitListenerContainerFactory : IServiceNameAware
    {
        public const string DEFAULT_RABBIT_LISTENER_CONTAINER_FACTORY_SERVICE_NAME = "rabbitListenerContainerFactory";

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
