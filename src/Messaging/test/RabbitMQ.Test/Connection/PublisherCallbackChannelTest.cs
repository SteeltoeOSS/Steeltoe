// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
                    catch (NullReferenceException)
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
