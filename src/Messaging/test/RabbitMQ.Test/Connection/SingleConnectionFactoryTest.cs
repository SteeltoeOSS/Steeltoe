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
            private AtomicInteger called;

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
