// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using Xunit;
using R = RabbitMQ.Client;

namespace Steeltoe.Messaging.Rabbit.Connection
{
    public class SingleConnectionFactoryTest : AbstractConnectionFactoryTest
    {
        [Fact]
        public void TestWithChannelListener()
        {
            var mockConnectionFactory = new Mock<R.IConnectionFactory>();
            var mockConnection = new Mock<R.IConnection>();
            var mockChannel = new Mock<R.IModel>();

            mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>())).Returns(mockConnection.Object);
            mockConnection.Setup(c => c.IsOpen).Returns(true);
            mockConnection.Setup(c => c.CreateModel()).Returns(mockChannel.Object);

            var called = new AtomicInteger(0);
            var connectionFactory = CreateConnectionFactory(mockConnectionFactory.Object, null);
            var listener = new TestListener(called);
            connectionFactory.SetChannelListeners(new List<IChannelListener>() { listener });
            var con = connectionFactory.CreateConnection();
            var channel = con.CreateChannel(false);
            Assert.Equal(1, called.Value);

            channel.Close();
            con.Close();

            mockConnection.Verify((c) => c.Close(), Times.Never);

            con = connectionFactory.CreateConnection();
            channel = con.CreateChannel(false);
            Assert.Equal(2, called.Value);

            connectionFactory.Destroy();
            mockConnection.Verify((c) => c.Close(It.IsAny<int>()), Times.AtLeastOnce);
            mockConnectionFactory.Verify(c => c.CreateConnection(It.IsAny<string>()));
        }

        private class TestListener : IChannelListener
        {
            private readonly AtomicInteger called;

            public TestListener(AtomicInteger called)
            {
                this.called = called;
            }

            public void OnCreate(IModel channel, bool transactional)
            {
                called.IncrementAndGet();
            }

            public void OnShutDown(ShutdownEventArgs args)
            {
                throw new NotImplementedException();
            }
        }

        protected override AbstractConnectionFactory CreateConnectionFactory(RabbitMQ.Client.IConnectionFactory mockConnectionFactory, ILoggerFactory loggerFactory)
        {
            var scf = new SingleConnectionFactory(mockConnectionFactory, loggerFactory);
            return scf;
        }
    }
}
