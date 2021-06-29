// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Moq;
using Steeltoe.Common.Util;
using System;
using Xunit;
using RC=RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Connection
{
    public class PublisherCallbackChannelTest
    {
        [Fact]
        public void ShutdownWhileCreate()
        {
            var mockChannel = new Mock<RC.IModel>();
            var npe = new AtomicBoolean();
            mockChannel.SetupAdd((m) => m.ModelShutdown += It.IsAny<EventHandler<RC.ShutdownEventArgs>>())
                .Callback<EventHandler<RC.ShutdownEventArgs>>((handler) =>
                {
                    try
                    {
                        handler.Invoke(null, new RC.ShutdownEventArgs(RC.ShutdownInitiator.Peer, (ushort)RabbitUtils.NotFound, string.Empty));
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
