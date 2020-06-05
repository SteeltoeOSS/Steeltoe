// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;
using R = RabbitMQ.Client;

namespace Steeltoe.Messaging.Rabbit.Connection
{
    public abstract class AbstractConnectionFactoryTest
    {
        [Fact]
        public void TestWithListener()
        {
            var mockConnectionFactory = new Mock<R.IConnectionFactory>();
            var mockConnection = new Mock<R.IConnection>();
            mockConnectionFactory.Setup((f) => f.CreateConnection(It.IsAny<string>())).Returns(mockConnection.Object);
            var mockLogger = new Mock<ILogger>();
            var connectionFactory = CreateConnectionFactory(mockConnectionFactory.Object, mockLogger.Object);
            var listener = new IncrementConnectionListener();
            connectionFactory.SetConnectionListeners(new List<IConnectionListener>() { listener });
            var con = connectionFactory.CreateConnection();
            Assert.Equal(1, listener.Called);
            mockLogger.Verify((l) => l.Log(LogLevel.Information, 0, It.IsAny<It.IsAnyType>(), null, (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.AtLeast(2));
            con.Close();
            Assert.Equal(1, listener.Called);
            mockConnection.Verify((c) => c.Close(It.IsAny<int>()), Times.Never);
            connectionFactory.CreateConnection();
            Assert.Equal(1, listener.Called);
            connectionFactory.Destroy();
            Assert.Equal(0, listener.Called);
            mockConnection.Verify((c) => c.Close(It.IsAny<int>()), Times.AtLeastOnce);
            mockConnectionFactory.Verify((f) => f.CreateConnection(It.IsAny<string>()), Times.Once);

            connectionFactory.SetAddresses("foo:5672,bar:5672");
            con = connectionFactory.CreateConnection();
            Assert.Equal(1, listener.Called);
            mockLogger.Verify((l) => l.Log(LogLevel.Information, 0, It.IsAny<It.IsAnyType>(), null, (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.AtLeast(4));
            con.Close();
            connectionFactory.Destroy();
            Assert.Equal(0, listener.Called);
        }

        [Fact]
        public void TestWithListenerRegisteredAfterOpen()
        {
            var mockConnectionFactory = new Mock<R.IConnectionFactory>();
            var mockConnection = new Mock<R.IConnection>();
            var listener = new IncrementConnectionListener();
            mockConnectionFactory.Setup((f) => f.CreateConnection(It.IsAny<string>())).Returns(mockConnection.Object);
            var connectionFactory = CreateConnectionFactory(mockConnectionFactory.Object, null);
            var con = connectionFactory.CreateConnection();
            Assert.Equal(0, listener.Called);
            connectionFactory.SetConnectionListeners(new List<IConnectionListener>() { listener });
            Assert.Equal(1, listener.Called);
            con.Close();
            Assert.Equal(1, listener.Called);
            mockConnection.Verify((c) => c.Close(It.IsAny<int>()), Times.Never);
            _ = connectionFactory.CreateConnection();
            Assert.Equal(1, listener.Called);
            connectionFactory.Destroy();
            Assert.Equal(0, listener.Called);
            mockConnection.Verify((c) => c.Close(It.IsAny<int>()), Times.AtLeastOnce);
            mockConnectionFactory.Verify((f) => f.CreateConnection(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void TestCloseInvalidConnection()
        {
            var mockConnectionFactory = new Mock<R.IConnectionFactory>();
            var mockConnection1 = new Mock<R.IConnection>();
            var mockConnection2 = new Mock<R.IConnection>();
            var mockChanel2 = new Mock<R.IModel>();
            mockConnectionFactory.SetupSequence((f) => f.CreateConnection(It.IsAny<string>()))
                .Returns(mockConnection1.Object)
                .Returns(mockConnection2.Object);
            mockConnection1.Setup((c) => c.IsOpen).Returns(false);
            mockConnection2.Setup((c) => c.CreateModel()).Returns(mockChanel2.Object);
            var connectionFactory = CreateConnectionFactory(mockConnectionFactory.Object, null);
            var connection = connectionFactory.CreateConnection();

            // the dead connection should be discarded
            _ = connection.CreateChannel();
            mockConnectionFactory.Verify((f) => f.CreateConnection(It.IsAny<string>()), Times.Exactly(2));
            mockConnection2.Verify((c) => c.CreateModel(), Times.Once);
            connectionFactory.Destroy();
            mockConnection2.Verify((c) => c.Close(It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public void TestDestroyBeforeUsed()
        {
            var mockConnectionFactory = new Mock<R.IConnectionFactory>();
            var connectionFactory = CreateConnectionFactory(mockConnectionFactory.Object, null);
            connectionFactory.Destroy();
            mockConnectionFactory.Verify((f) => f.CreateConnection(It.IsAny<string>()), Times.Never);
        }

        protected abstract AbstractConnectionFactory CreateConnectionFactory(R.IConnectionFactory mockConnectionFactory, ILogger logger);

        protected class IncrementConnectionListener : IConnectionListener
        {
            public int Called;

            public void OnClose(IConnection connection)
            {
                Interlocked.Decrement(ref Called);
            }

            public void OnCreate(IConnection connection)
            {
                Interlocked.Increment(ref Called);
            }

            public void OnShutDown(ShutdownEventArgs args)
            {
            }
        }
    }
}
