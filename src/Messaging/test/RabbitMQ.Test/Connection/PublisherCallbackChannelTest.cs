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

using Moq;
using RabbitMQ.Client;
using Steeltoe.Common.Util;
using System;
using Xunit;

namespace Steeltoe.Messaging.Rabbit.Connection
{
    public class PublisherCallbackChannelTest
    {
        [Fact]
        public void ShutdownWhileCreate()
        {
            var mockChannel = new Mock<IModel>();
            var npe = new AtomicBoolean();
            mockChannel.SetupAdd((m) => m.ModelShutdown += It.IsAny<EventHandler<ShutdownEventArgs>>())
                .Callback<EventHandler<ShutdownEventArgs>>((handler) =>
                {
                    try
                    {
                        handler.Invoke(null, new ShutdownEventArgs(ShutdownInitiator.Peer, (ushort)RabbitUtils.NotFound, string.Empty));
                    }
                    catch (NullReferenceException e)
                    {
                        npe.Value = true;
                    }
                });
            var channel = new PublisherCallbackChannel(mockChannel.Object);
            Assert.False(npe.Value);
            channel.Close();
        }
    }
}
