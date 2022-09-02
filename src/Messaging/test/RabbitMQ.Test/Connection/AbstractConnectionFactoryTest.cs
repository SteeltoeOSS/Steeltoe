// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Connection;

public abstract class AbstractConnectionFactoryTest
{
    [Fact]
    public void TestWithListener()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnection = new Mock<RC.IConnection>();
        mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>())).Returns(mockConnection.Object);

        // var mockLogger = new Mock<ILoggerFactory>();
        AbstractConnectionFactory connectionFactory = CreateConnectionFactory(mockConnectionFactory.Object);
        var listener = new IncrementConnectionListener();

        connectionFactory.SetConnectionListeners(new List<IConnectionListener>
        {
            listener
        });

        IConnection con = connectionFactory.CreateConnection();
        Assert.Equal(1, listener.Called);

        // mockLogger.Verify((l) => l.Log(LogLevel.Information, 0, It.IsAny<It.IsAnyType>(), null, (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.AtLeast(2));
        con.Close();
        Assert.Equal(1, listener.Called);
        mockConnection.Verify(c => c.Close(It.IsAny<int>()), Times.Never);
        connectionFactory.CreateConnection();
        Assert.Equal(1, listener.Called);
        connectionFactory.Destroy();
        Assert.Equal(0, listener.Called);
        mockConnection.Verify(c => c.Close(It.IsAny<int>()), Times.AtLeastOnce);
        mockConnectionFactory.Verify(f => f.CreateConnection(It.IsAny<string>()), Times.Once);

        connectionFactory.SetAddresses("foo:5672,bar:5672");
        con = connectionFactory.CreateConnection();
        Assert.Equal(1, listener.Called);

        // mockLogger.Verify((l) => l.Log(LogLevel.Information, 0, It.IsAny<It.IsAnyType>(), null, (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.AtLeast(4));
        con.Close();
        connectionFactory.Destroy();
        Assert.Equal(0, listener.Called);
    }

    [Fact]
    public void TestWithListenerRegisteredAfterOpen()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnection = new Mock<RC.IConnection>();
        var listener = new IncrementConnectionListener();
        mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>())).Returns(mockConnection.Object);
        AbstractConnectionFactory connectionFactory = CreateConnectionFactory(mockConnectionFactory.Object);
        IConnection con = connectionFactory.CreateConnection();
        Assert.Equal(0, listener.Called);

        connectionFactory.SetConnectionListeners(new List<IConnectionListener>
        {
            listener
        });

        Assert.Equal(1, listener.Called);
        con.Close();
        Assert.Equal(1, listener.Called);
        mockConnection.Verify(c => c.Close(It.IsAny<int>()), Times.Never);
        _ = connectionFactory.CreateConnection();
        Assert.Equal(1, listener.Called);
        connectionFactory.Destroy();
        Assert.Equal(0, listener.Called);
        mockConnection.Verify(c => c.Close(It.IsAny<int>()), Times.AtLeastOnce);
        mockConnectionFactory.Verify(f => f.CreateConnection(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void TestCloseInvalidConnection()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnection1 = new Mock<RC.IConnection>();
        var mockConnection2 = new Mock<RC.IConnection>();
        var mockChanel2 = new Mock<RC.IModel>();
        mockConnectionFactory.SetupSequence(f => f.CreateConnection(It.IsAny<string>())).Returns(mockConnection1.Object).Returns(mockConnection2.Object);
        mockConnection1.Setup(c => c.IsOpen).Returns(false);
        mockConnection2.Setup(c => c.CreateModel()).Returns(mockChanel2.Object);
        AbstractConnectionFactory connectionFactory = CreateConnectionFactory(mockConnectionFactory.Object);
        IConnection connection = connectionFactory.CreateConnection();

        // the dead connection should be discarded
        _ = connection.CreateChannel();
        mockConnectionFactory.Verify(f => f.CreateConnection(It.IsAny<string>()), Times.Exactly(2));
        mockConnection2.Verify(c => c.CreateModel(), Times.Once);
        connectionFactory.Destroy();
        mockConnection2.Verify(c => c.Close(It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public void TestDestroyBeforeUsed()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        AbstractConnectionFactory connectionFactory = CreateConnectionFactory(mockConnectionFactory.Object);
        connectionFactory.Destroy();
        mockConnectionFactory.Verify(f => f.CreateConnection(It.IsAny<string>()), Times.Never);
    }

    protected abstract AbstractConnectionFactory CreateConnectionFactory(RC.IConnectionFactory connectionFactory, ILoggerFactory loggerFactory = null);

    protected class IncrementConnectionListener : IConnectionListener
    {
        private int _called;

        public int Called => _called;

        public void OnClose(IConnection connection)
        {
            Interlocked.Decrement(ref _called);
        }

        public void OnCreate(IConnection connection)
        {
            Interlocked.Increment(ref _called);
        }

        public void OnShutDown(RC.ShutdownEventArgs args)
        {
        }
    }
}
