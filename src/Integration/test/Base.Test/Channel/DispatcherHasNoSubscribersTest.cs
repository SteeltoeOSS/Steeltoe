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

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Integration.Handler;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using System;
using Xunit;

namespace Steeltoe.Integration.Channel.Test
{
    public class DispatcherHasNoSubscribersTest
    {
        [Fact]
        public void OneChannel()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            var provider = services.BuildServiceProvider();
            var noSubscribersChannel = new DirectChannel(provider);
            try
            {
                noSubscribersChannel.Send(new GenericMessage<string>("Hello, world!"));
                throw new Exception("Exception expected");
            }
            catch (MessagingException e)
            {
                Assert.Contains("Dispatcher has no subscribers", e.Message);
            }
        }

        [Fact]
        public void BridgedChannel()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            var provider = services.BuildServiceProvider();
            var noSubscribersChannel = new DirectChannel(provider);
            var subscribedChannel = new DirectChannel(provider);
            var bridgeHandler = new BridgeHandler(provider);
            bridgeHandler.OutputChannel = noSubscribersChannel;
            subscribedChannel.Subscribe(bridgeHandler);
            try
            {
                subscribedChannel.Send(new GenericMessage<string>("Hello, world!"));
                throw new Exception("Exception expected");
            }
            catch (MessagingException e)
            {
                Assert.Contains("Dispatcher has no subscribers", e.Message);
            }
        }
    }
}
