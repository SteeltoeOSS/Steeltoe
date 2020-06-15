// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
