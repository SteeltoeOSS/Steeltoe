// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client.Events;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Connection;

public class CachingConnectionFactoryTest : AbstractConnectionFactoryTest
{
    [Fact]
    public void TestWithConnectionFactoryDefaults()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnection = new Mock<RC.IConnection>();
        var mockChanel = new Mock<RC.IModel>();
        mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>())).Returns(mockConnection.Object);
        mockConnection.Setup(c => c.CreateModel()).Returns(mockChanel.Object);
        mockConnection.Setup(c => c.IsOpen).Returns(true);
        mockChanel.Setup(c => c.IsOpen).Returns(true);
        var ccf = new CachingConnectionFactory(mockConnectionFactory.Object);
        var con = ccf.CreateConnection();
        var channel = con.CreateChannel();
        channel.Close();  // should be ignored, and placed into channel cache.
        con.Close(); // should be ignored
        var con2 = ccf.CreateConnection();

        // Will retrieve same channel object that was just put into channel cache
        var channel2 = con2.CreateChannel();
        channel2.Close(); // should be ignored
        con2.Close(); // should be ignored

        Assert.Same(con, con2);
        Assert.Same(channel, channel2);
        mockConnection.Verify(c => c.Close(), Times.Never);
        mockChanel.Verify(c => c.Close(), Times.Never);
    }

    [Fact]
    public void TestPublisherConnection()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnection = new Mock<RC.IConnection>();
        var mockChanel = new Mock<RC.IModel>();
        mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>())).Returns(mockConnection.Object);
        mockConnection.Setup(c => c.CreateModel()).Returns(mockChanel.Object);
        mockConnection.Setup(c => c.IsOpen).Returns(true);
        mockChanel.Setup(c => c.IsOpen).Returns(true);

        var ccf = new CachingConnectionFactory(mockConnectionFactory.Object);
        var con = ccf.PublisherConnectionFactory.CreateConnection();
        var channel = con.CreateChannel();
        channel.Close();  // should be ignored, and placed into channel cache.
        con.Close(); // should be ignored
        var con2 = ccf.PublisherConnectionFactory.CreateConnection();

        // Will retrieve same channel object that was just put into channel cache
        var channel2 = con2.CreateChannel();
        channel2.Close(); // should be ignored
        con2.Close(); // should be ignored

        Assert.Same(con, con2);
        Assert.Same(channel, channel2);
        mockConnection.Verify(c => c.Close(), Times.Never);
        mockChanel.Verify(c => c.Close(), Times.Never);
    }

    [Fact]
    public void TestWithConnectionFactoryCacheSize()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnection = new Mock<RC.IConnection>();
        var mockChannel1 = new Mock<RC.IModel>();
        var mockChanel2 = new Mock<RC.IModel>();
        var mockTxChanel = new Mock<RC.IModel>();

        mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>())).Returns(mockConnection.Object);
        mockConnection.Setup(c => c.IsOpen).Returns(true);
        mockConnection.SetupSequence(c => c.CreateModel())
            .Returns(mockChannel1.Object)
            .Returns(mockChanel2.Object)
            .Returns(mockTxChanel.Object);

        mockChannel1.Setup(c => c.BasicGet("foo", false)).Returns(new RC.BasicGetResult(0, false, null, null, 1, null, null));
        mockChanel2.Setup(c => c.BasicGet("foo", false)).Returns(new RC.BasicGetResult(0, false, null, null, 1, null, null));
        mockChannel1.Setup(c => c.IsOpen).Returns(true);
        mockChanel2.Setup(c => c.IsOpen).Returns(true);
        var ccf = new CachingConnectionFactory(mockConnectionFactory.Object)
        {
            ChannelCacheSize = 2
        };
        var con = ccf.CreateConnection();
        var channel1 = con.CreateChannel();
        var channel2 = con.CreateChannel();
        var txChannel = (IChannelProxy)con.CreateChannel(true);
        Assert.True(txChannel.IsTransactional);
        mockTxChanel.Verify(c => c.TxSelect(), Times.Once);
        txChannel.Close();
        channel1.BasicGet("foo", true);
        channel2.BasicGet("bar", true);
        channel1.Close(); // should be ignored, and add last into channel cache.
        channel2.Close(); // should be ignored, and add last into channel cache.

        var ch1 = con.CreateChannel(); // remove first entry in cache
        var ch2 = con.CreateChannel(); // remove first entry in cache

        Assert.NotSame(ch1, ch2);
        Assert.Same(ch1, channel1);
        Assert.Same(ch2, channel2);

        ch1.Close();
        ch2.Close();

        mockConnection.Verify(c => c.CreateModel(), Times.Exactly(3));

        con.Close();
        mockConnection.Verify(c => c.Close(), Times.Never);
        mockChannel1.Verify(c => c.Close(), Times.Never);
        mockChanel2.Verify(c => c.Close(), Times.Never);
    }

    [Fact]
    public void TestCacheSizeExceeded()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnection = new Mock<RC.IConnection>();
        var mockChannel1 = new Mock<RC.IModel>();
        var mockChanel2 = new Mock<RC.IModel>();
        var mockChanel3 = new Mock<RC.IModel>();
        mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>())).Returns(mockConnection.Object);
        mockConnection.SetupSequence(c => c.CreateModel())
            .Returns(mockChannel1.Object)
            .Returns(mockChanel2.Object)
            .Returns(mockChanel3.Object);
        mockConnection.Setup(c => c.IsOpen).Returns(true);

        mockChannel1.Setup(c => c.IsOpen).Returns(true);
        mockChanel2.Setup(c => c.IsOpen).Returns(true);
        mockChanel3.Setup(c => c.IsOpen).Returns(true);

        var ccf = new CachingConnectionFactory(mockConnectionFactory.Object)
        {
            ChannelCacheSize = 1
        };

        var con = ccf.CreateConnection();

        var channel1 = con.CreateChannel();

        // cache size is 1, but the other connection is not released yet so this
        // creates a new one
        var channel2 = con.CreateChannel();
        Assert.NotSame(channel1, channel2);

        // should be ignored, and added last into channel cache.
        channel1.Close();

        // should be physically closed
        channel2.Close();

        // remove first entry in cache (channel1)
        var ch1 = con.CreateChannel();

        // create a new channel
        var ch2 = con.CreateChannel();

        Assert.NotSame(ch1, ch2);
        Assert.Same(channel1, ch1);
        Assert.NotSame(channel2, ch2);

        ch1.Close();
        ch2.Close();

        mockConnection.Verify(c => c.CreateModel(), Times.Exactly(3));

        con.Close();
        mockConnection.Verify(c => c.Close(), Times.Never);
        mockChannel1.Verify(c => c.Close(), Times.Never);
        mockChanel2.Verify(c => c.Close(), Times.AtLeastOnce);
        mockChanel3.Verify(c => c.Close(), Times.AtLeastOnce);
    }

    [Fact]
    public void TestCheckoutLimit()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnection = new Mock<RC.IConnection>();
        var mockChannel1 = new Mock<RC.IModel>();

        mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>())).Returns(mockConnection.Object);
        mockConnection.Setup(c => c.CreateModel()).Returns(mockChannel1.Object);
        mockConnection.Setup(c => c.IsOpen).Returns(true);
        mockChannel1.Setup(c => c.IsOpen).Returns(true);

        var ccf = new CachingConnectionFactory(mockConnectionFactory.Object)
        {
            ChannelCacheSize = 1,
            ChannelCheckoutTimeout = 10
        };

        var con = ccf.CreateConnection();

        var channel1 = con.CreateChannel();
        try
        {
            con.CreateChannel();
            throw new Exception("Exception expected");
        }
        catch (RabbitTimeoutException)
        {
            // Intentionally left empty.
        }

        // should be ignored, and added last into channel cache.
        channel1.Close();

        // remove first entry in cache (channel1)
        var ch1 = con.CreateChannel();

        Assert.Same(channel1, ch1);
        ch1.Close();
        mockConnection.Verify(c => c.CreateModel(), Times.Once);
        con.Close(); // should be ignored

        mockConnection.Verify(c => c.Close(), Times.Never);
        mockChannel1.Verify(c => c.Close(), Times.Never);

        ccf.Destroy();
    }

    [Fact]
    public void TestCheckoutLimitWithFailures()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnection = new Mock<RC.IConnection>();
        var mockChannel1 = new Mock<RC.IModel>();

        var brokerDown = new AtomicBoolean(false);

        mockConnectionFactory.SetupSequence(f => f.CreateConnection(It.IsAny<string>()))
            .Returns(mockConnection.Object)
            .Throws(new RabbitConnectException(null)) // Happens when broker down
            .Returns(mockConnection.Object);

        mockConnection.Setup(c => c.CreateModel()).Returns(mockChannel1.Object);

        mockConnection.Setup(c => c.IsOpen).Returns(() => !brokerDown.Value);
        mockChannel1.Setup(c => c.IsOpen).Returns(() => !brokerDown.Value);

        var ccf = new CachingConnectionFactory(mockConnectionFactory.Object)
        {
            ChannelCacheSize = 1,
            ChannelCheckoutTimeout = 10
        };

        var con = ccf.CreateConnection(); // .Returns(mockConnection.Object)

        var channel1 = con.CreateChannel();
        try
        {
            con.CreateChannel();
            throw new Exception("Exception expected");
        }
        catch (RabbitTimeoutException)
        {
            // Intentionally left empty.
        }

        channel1.Close();
        var ch1 = con.CreateChannel();
        Assert.Same(channel1, ch1);

        ch1.Close();

        // Connection will report not open, will try to create new one
        brokerDown.Value = true;
        try
        {
            // .Throws(new AmqpConnectException(null)) thrown
            con.CreateChannel();
            throw new Exception("Exception expected");
        }
        catch (RabbitConnectException)
        {
            // Intentionally left empty.
        }

        brokerDown.Value = true;

        // Will try to create new connection and will succeed
        ch1 = con.CreateChannel();
        ch1.Close();

        ccf.Destroy();
    }

    [Fact]
    public async Task TestConnectionLimit()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnection = new Mock<RC.IConnection>();
        mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>())).Returns(mockConnection.Object);
        mockConnection.Setup(c => c.IsOpen).Returns(true);

        var ccf = new CachingConnectionFactory(mockConnectionFactory.Object, false, CachingConnectionFactory.CachingMode.Connection)
        {
            ChannelCacheSize = 1,
            ConnectionLimit = 1,
            ChannelCheckoutTimeout = 10
        };

        var con1 = ccf.CreateConnection(); // .Returns(mockConnection.Object)
        try
        {
            ccf.CreateConnection();
            throw new Exception("Exception expected");
        }
        catch (RabbitTimeoutException)
        {
            // Intentionally left empty.
        }

        // should be ignored, and added to cache
        con1.Close();
        var con2 = ccf.CreateConnection();
        Assert.Same(con1, con2);

        var latch2 = new CountdownEvent(1);
        var latch1 = new CountdownEvent(1);
        var connection = new AtomicReference<IConnection>();

        ccf.ChannelCheckoutTimeout = 30_000;
        _ = Task.Run(() =>
        {
            latch1.Signal();
            connection.Value = ccf.CreateConnection();
            latch2.Signal();
        });

        Assert.True(latch1.Wait(TimeSpan.FromSeconds(10)));
        await Task.Delay(100);
        con2.Close();

        Assert.True(latch2.Wait(TimeSpan.FromSeconds(10)));
        Assert.Same(con2, connection.Value);
        ccf.Destroy();
    }

    [Fact]
    public void TestCheckoutsWithRefreshedConnectionModeChannel()
    {
        TestCheckoutsWithRefreshedConnectionGuts(CachingConnectionFactory.CachingMode.Channel);
    }

    [Fact]
    public void TestCheckoutsWithRefreshedConnectionModeConnection()
    {
        TestCheckoutsWithRefreshedConnectionGuts(CachingConnectionFactory.CachingMode.Connection);
    }

    [Fact]
    public void TestCheckoutLimitWithRelease()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnection = new Mock<RC.IConnection>();
        var mockChannel1 = new Mock<RC.IModel>();
        mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>()))
            .Returns(mockConnection.Object);
        mockConnection.Setup(c => c.CreateModel()).Returns(mockChannel1.Object);
        mockConnection.Setup(c => c.IsOpen).Returns(true);
        mockChannel1.Setup(c => c.IsOpen).Returns(true);

        var ccf = new CachingConnectionFactory(mockConnectionFactory.Object)
        {
            ChannelCacheSize = 1,
            ChannelCheckoutTimeout = 10_000
        };

        var con = ccf.CreateConnection();
        var channelOne = new AtomicReference<RC.IModel>();
        var latch = new CountdownEvent(1);
        _ = Task.Run(async () =>
        {
            var channel1 = con.CreateChannel();
            latch.Signal();
            channelOne.Value = channel1;
            try
            {
                await Task.Delay(100);
                channel1.Close();
            }
            catch (Exception)
            {
                // Ignore
            }
        });
        Assert.True(latch.Wait(TimeSpan.FromSeconds(10)));
        var channel2 = con.CreateChannel();
        Assert.Same(channelOne.Value, channel2);

        channel2.Close();

        mockConnection.Verify(c => c.Close(), Times.Never);
        mockChannel1.Verify(c => c.Close(), Times.Never);

        ccf.Destroy();
    }

    [Fact]
    public async Task TestCheckoutLimitWithPublisherConfirmsLogical()
    {
        await TestCheckoutLimitWithPublisherConfirms(false);
    }

    [Fact]
    public async Task TestCheckoutLimitWithPublisherConfirmsPhysical()
    {
        await TestCheckoutLimitWithPublisherConfirms(true);
    }

    [Fact]
    public void TestCheckoutLimitWithPublisherConfirmsLogicalAlreadyCloses()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnection = new Mock<RC.IConnection>();
        var mockChannel1 = new Mock<RC.IModel>();
        var mockProperties = new Mock<RC.IBasicProperties>();
        mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>()))
            .Returns(mockConnection.Object);
        mockConnection.Setup(c => c.CreateModel()).Returns(mockChannel1.Object);
        mockConnection.Setup(c => c.IsOpen).Returns(true);

        var open = new AtomicBoolean(true);
        mockChannel1.Setup(c => c.IsOpen).Returns(() => open.Value);
        mockChannel1.Setup(c => c.CreateBasicProperties()).Returns(mockProperties.Object);
        mockChannel1.Setup(c => c.NextPublishSeqNo).Returns(1);
        mockChannel1.Setup(c => c.BasicPublish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<RC.IBasicProperties>(), It.IsAny<byte[]>()))
            .Callback(() => open.Value = false);
        var ccf = new CachingConnectionFactory(mockConnectionFactory.Object)
        {
            ChannelCacheSize = 1,
            ChannelCheckoutTimeout = 1,
            PublisherConfirmType = CachingConnectionFactory.ConfirmType.Correlated
        };
        var rabbitTemplate = new RabbitTemplate(ccf);
        rabbitTemplate.ConvertAndSend("foo", "bar");
        open.Value = true;
        rabbitTemplate.ConvertAndSend("foo", "bar");
        mockChannel1.Verify(c => c.BasicPublish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<RC.IBasicProperties>(), It.IsAny<byte[]>()), Times.Exactly(2));
    }

    [Fact]
    public void TestReleaseWithForcedPhysicalClose()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnection = new Mock<RC.IConnection>();
        var mockChannel1 = new Mock<RC.IModel>();
        mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>())).Returns(mockConnection.Object);
        mockConnection.Setup(c => c.CreateModel()).Returns(mockChannel1.Object);
        mockConnection.Setup(c => c.IsOpen).Returns(true);
        mockChannel1.Setup(c => c.IsOpen).Returns(true);
        var ccf = new CachingConnectionFactory(mockConnectionFactory.Object)
        {
            ChannelCacheSize = 1,
            ChannelCheckoutTimeout = 10
        };

        var con = ccf.CreateConnection();

        var channel1 = con.CreateChannel();

        Assert.Single(ccf.CheckoutPermits.Values);
        var slim = ccf.CheckoutPermits.Values.Single();
        Assert.Equal(0, slim.CurrentCount);

        channel1.Close();
        con.Close();
        Assert.Equal(1, slim.CurrentCount);

        channel1 = con.CreateChannel();
        RabbitUtils.SetPhysicalCloseRequired(channel1, true);
        Assert.Equal(0, slim.CurrentCount);

        channel1.Close();
        RabbitUtils.SetPhysicalCloseRequired(channel1, false);
        con.Close();

        mockChannel1.Verify(c => c.Close());
        mockConnection.Verify(c => c.Close(), Times.Never);
        Assert.Equal(1, slim.CurrentCount);

        ccf.Destroy();
        Assert.Equal(1, slim.CurrentCount);
    }

    [Fact]
    public void TestDoubleLogicalClose()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnection = new Mock<RC.IConnection>();
        var mockChannel1 = new Mock<RC.IModel>();
        mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>())).Returns(mockConnection.Object);
        mockConnection.Setup(c => c.CreateModel()).Returns(mockChannel1.Object);
        mockConnection.Setup(c => c.IsOpen).Returns(true);
        mockChannel1.Setup(c => c.IsOpen).Returns(true);
        var ccf = new CachingConnectionFactory(mockConnectionFactory.Object)
        {
            ChannelCacheSize = 1,
            ChannelCheckoutTimeout = 10
        };

        var con = ccf.CreateConnection();

        var channel1 = con.CreateChannel();

        Assert.Single(ccf.CheckoutPermits.Values);
        var slim = ccf.CheckoutPermits.Values.Single();
        Assert.Equal(0, slim.CurrentCount);

        channel1.Close();
        Assert.Equal(1, slim.CurrentCount);

        channel1.Close(); // double close of proxy
        Assert.Equal(1, slim.CurrentCount);

        con.Close();

        mockChannel1.Verify(c => c.Close(), Times.Never);
        mockConnection.Verify(c => c.Close(), Times.Never);

        ccf.Destroy();
        Assert.Equal(1, slim.CurrentCount);
    }

    [Fact]
    public void TestCacheSizeExceededAfterClose()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnection = new Mock<RC.IConnection>();
        var mockChannel1 = new Mock<RC.IModel>();
        var mockChannel2 = new Mock<RC.IModel>();
        mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>())).Returns(mockConnection.Object);
        mockConnection.SetupSequence(c => c.CreateModel()).Returns(mockChannel1.Object).Returns(mockChannel2.Object);
        mockConnection.Setup(c => c.IsOpen).Returns(true);
        mockChannel1.Setup(c => c.IsOpen).Returns(true);
        mockChannel2.Setup(c => c.IsOpen).Returns(true);

        var ccf = new CachingConnectionFactory(mockConnectionFactory.Object)
        {
            ChannelCacheSize = 1
        };

        var con = ccf.CreateConnection();

        var channel1 = con.CreateChannel();
        channel1.Close();

        var channel2 = con.CreateChannel();
        channel2.Close();

        Assert.Same(channel1, channel2);

        var ch1 = con.CreateChannel(); // remove first entry in cache

        // (channel1)
        var ch2 = con.CreateChannel(); // create new channel

        Assert.NotSame(ch1, ch2);
        Assert.Same(ch1, channel1);
        Assert.NotSame(ch2, channel2);

        ch1.Close();
        ch2.Close();

        mockConnection.Verify(c => c.CreateModel(), Times.Exactly(2));
        con.Close(); // should be ignored

        mockConnection.Verify(c => c.Close(), Times.Never);
        mockChannel1.Verify(c => c.Close(), Times.Never);
        mockChannel2.Verify(c => c.Close(), Times.AtLeastOnce());
    }

    [Fact]
    public void TestTransactionalAndNonTransactionalChannelsSegregated()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnection = new Mock<RC.IConnection>();
        var mockChannel1 = new Mock<RC.IModel>();
        var mockChannel2 = new Mock<RC.IModel>();
        mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>())).Returns(mockConnection.Object);
        mockConnection.SetupSequence(c => c.CreateModel()).Returns(mockChannel1.Object).Returns(mockChannel2.Object);
        mockConnection.Setup(c => c.IsOpen).Returns(true);
        mockChannel1.Setup(c => c.IsOpen).Returns(true);
        mockChannel2.Setup(c => c.IsOpen).Returns(true);

        var ccf = new CachingConnectionFactory(mockConnectionFactory.Object)
        {
            ChannelCacheSize = 1
        };

        var con = ccf.CreateConnection();
        var channel1 = con.CreateChannel(true);
        channel1.TxSelect();
        channel1.Close(); // should be ignored, and add last into channel cache.

        // When a channel is created as non-transactional we should create a new one.
        var channel2 = con.CreateChannel();
        channel2.Close(); // should be ignored, and add last into channel cache.
        Assert.NotSame(channel1, channel2);

        var ch1 = con.CreateChannel(true); // remove first entry in cache (channel1)
        var ch2 = con.CreateChannel(); // create new channel

        Assert.NotSame(ch1, ch2);
        Assert.Same(channel1, ch1);
        Assert.Same(channel2, ch2);

        ch1.Close();
        ch2.Close();

        mockConnection.Verify(c => c.CreateModel(), Times.Exactly(2));
        con.Close(); // should be ignored

        mockConnection.Verify(c => c.Close(), Times.Never);
        mockChannel1.Verify(c => c.Close(), Times.Never);
        mockChannel2.Verify(c => c.Close(), Times.Never);

        Assert.Single(ccf.CachedChannelsNonTransactional);
        Assert.Single(ccf.CachedChannelsTransactional);
    }

    [Fact]
    public void TestWithConnectionFactoryDestroy()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnection1 = new Mock<RC.IConnection>();
        var mockConnection2 = new Mock<RC.IConnection>();

        var mockChannel1 = new Mock<RC.IModel>();
        var mockChannel2 = new Mock<RC.IModel>();
        var mockChannel3 = new Mock<RC.IModel>();

        mockConnectionFactory.SetupSequence(f => f.CreateConnection(It.IsAny<string>()))
            .Returns(mockConnection1.Object)
            .Returns(mockConnection2.Object);

        mockConnection1.SetupSequence(c => c.CreateModel())
            .Returns(mockChannel1.Object)
            .Returns(mockChannel2.Object);
        mockConnection1.Setup(c => c.IsOpen).Returns(true);

        mockConnection2.SetupSequence(c => c.CreateModel())
            .Returns(mockChannel3.Object);
        mockConnection2.Setup(c => c.IsOpen).Returns(true);

        mockChannel1.Setup(c => c.IsOpen).Returns(true);
        mockChannel2.Setup(c => c.IsOpen).Returns(true);
        mockChannel3.Setup(c => c.IsOpen).Returns(true);

        var ccf = new CachingConnectionFactory(mockConnectionFactory.Object)
        {
            ChannelCacheSize = 2
        };
        var con = ccf.CreateConnection();

        // This will return a proxy that suppresses calls to close
        var channel1 = con.CreateChannel();
        var channel2 = con.CreateChannel();

        // Should be ignored, and add last into channel cache.
        channel1.Close();
        channel2.Close();

        // remove first entry in cache (channel1)
        var ch1 = con.CreateChannel();

        // remove first entry in cache (channel2)
        var ch2 = con.CreateChannel();

        Assert.Same(channel1, ch1);
        Assert.Same(channel2, ch2);

        var target1 = ((IChannelProxy)ch1).TargetChannel;
        var target2 = ((IChannelProxy)ch2).TargetChannel;

        Assert.NotSame(target1, target2);

        ch1.Close();
        ch2.Close();
        con.Close(); // should be ignored

        // com.rabbitmq.client.Connection conDelegate = targetDelegate(con);
        var asProxy = con as CachingConnectionFactory.ChannelCachingConnectionProxy;
        var conDelegate = asProxy.TargetConnection.Connection;

        ccf.Destroy();

        mockConnection1.Verify(c => c.CreateModel(), Times.Exactly(2));
        mockConnection1.Verify(c => c.Close(It.IsAny<int>()));

        mockChannel2.Verify(c => c.Close());

        var con1 = ccf.CreateConnection();

        // assertThat(targetDelegate(con1)).isNotSameAs(conDelegate);
        var asProxy1 = con1 as CachingConnectionFactory.ChannelCachingConnectionProxy;
        var conDelegate1 = asProxy1.TargetConnection.Connection;
        Assert.NotSame(conDelegate, conDelegate1);

        var channel3 = con.CreateChannel();

        Assert.NotSame(channel1, channel3);
        Assert.NotSame(channel2, channel3);
    }

    [Fact]
    public void TestWithChannelListener()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnection = new Mock<RC.IConnection>();
        var mockChannel1 = new Mock<RC.IModel>();
        mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>())).Returns(mockConnection.Object);
        mockConnection.Setup(c => c.CreateModel()).Returns(mockChannel1.Object);
        mockConnection.Setup(c => c.IsOpen).Returns(true);
        mockChannel1.Setup(c => c.IsOpen).Returns(true);

        var called = new AtomicInteger(0);
        var connectionFactory = CreateConnectionFactory(mockConnectionFactory.Object);
        connectionFactory.SetChannelListeners(new List<IChannelListener> { new TestWithChannelListenerListener(called) });
        ((CachingConnectionFactory)connectionFactory).ChannelCacheSize = 1;

        var con = connectionFactory.CreateConnection();
        var channel = con.CreateChannel();
        Assert.Equal(1, called.Value);

        channel.Close();
        con.Close();

        mockConnection.Verify(c => c.Close(), Times.Never);
        connectionFactory.CreateConnection();
        con.CreateChannel();
        Assert.Equal(1, called.Value);

        connectionFactory.Destroy();
        mockConnection.Verify(c => c.Close(It.IsAny<int>()));
        mockConnectionFactory.Verify(c => c.CreateConnection(It.IsAny<string>()));
    }

    [Fact]
    public void TestWithConnectionListener()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnection1 = new Mock<RC.IConnection>();
        mockConnection1.Setup(c => c.ToString()).Returns("conn1");
        var mockConnection2 = new Mock<RC.IConnection>();
        mockConnection2.Setup(c => c.ToString()).Returns("conn2");
        var mockChannel1 = new Mock<RC.IModel>();
        mockConnectionFactory.SetupSequence(f => f.CreateConnection(It.IsAny<string>()))
            .Returns(mockConnection1.Object)
            .Returns(mockConnection2.Object);
        mockConnection1.Setup(c => c.IsOpen).Returns(true);
        mockChannel1.Setup(c => c.IsOpen).Returns(true);
        mockConnection1.Setup(c => c.CreateModel()).Returns(mockChannel1.Object);
        mockConnection2.Setup(c => c.CreateModel()).Returns(mockChannel1.Object);

        var created = new AtomicReference<IConnection>();
        var closed = new AtomicReference<IConnection>();
        var timesClosed = new AtomicInteger(0);
        var connectionFactory = CreateConnectionFactory(mockConnectionFactory.Object);
        connectionFactory.AddConnectionListener(new TestWithConnectionListenerListener(created, closed, timesClosed));
        ((CachingConnectionFactory)connectionFactory).ChannelCacheSize = 1;
        var con = connectionFactory.CreateConnection();
        var channel = con.CreateChannel();
        Assert.Same(created.Value, con);
        channel.Close();

        con.Close();
        mockConnection1.Verify(c => c.Close(), Times.Never);

        var same = connectionFactory.CreateConnection();
        channel = con.CreateChannel();
        Assert.Same(same, con);
        channel.Close();

        var asProxy = con as CachingConnectionFactory.ChannelCachingConnectionProxy;
        var conDelegate = asProxy.TargetConnection.Connection;

        mockConnection1.Setup(c => c.IsOpen).Returns(false);
        mockChannel1.Setup(c => c.IsOpen).Returns(false);

        channel.BasicCancel("foo");
        channel.Close();
        Assert.Equal(1, timesClosed.Value);

        var notSame = connectionFactory.CreateConnection();

        var asProxy1 = notSame as CachingConnectionFactory.ChannelCachingConnectionProxy;
        var conDelegate1 = asProxy1.TargetConnection.Connection;
        Assert.NotSame(conDelegate, conDelegate1);
        Assert.Same(closed.Value, con);
        Assert.Same(created.Value, notSame);

        connectionFactory.Destroy();
        mockConnection2.Verify(c => c.Close(It.IsAny<int>()), Times.AtLeastOnce);
        Assert.Same(closed.Value, notSame);
        Assert.Equal(2, timesClosed.Value);

        mockConnectionFactory.Verify(f => f.CreateConnection(It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    public void TestWithConnectionFactoryCachedConnection()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnections = new List<Mock<RC.IConnection>>();
        var mockChannels = new List<Mock<RC.IModel>>();

        var connectionNumber = new AtomicInteger();
        var channelNumber = new AtomicInteger();

        mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>()))
            .Callback(() =>
            {
                var connection = new Mock<RC.IConnection>();
                connection.Setup(c => c.CreateModel())
                    .Callback(() =>
                    {
                        var channel = new Mock<RC.IModel>();
                        channel.Setup(c => c.IsOpen).Returns(true);
                        var channelNum = channelNumber.IncrementAndGet();
                        channel.Setup(c => c.ToString()).Returns($"mockChannel{connectionNumber}:{channelNum}");
                        mockChannels.Add(channel);
                    })
                    .Returns(() => mockChannels[channelNumber.Value - 1].Object);
                var connectionNum = connectionNumber.IncrementAndGet();
                connection.Setup(c => c.ToString()).Returns($"mockConnection{connectionNum}");
                connection.Setup(c => c.IsOpen).Returns(true);
                mockConnections.Add(connection);
            })
            .Returns(() => mockConnections[connectionNumber.Value - 1].Object);
        var ccf = new CachingConnectionFactory(mockConnectionFactory.Object, false, CachingConnectionFactory.CachingMode.Connection);

        Assert.Empty(ccf.AllocatedConnections);
        Assert.Empty(ccf.IdleConnections);

        var createNotification = new AtomicReference<RC.IConnection>();
        var closedNotification = new AtomicReference<RC.IConnection>();
        ccf.SetConnectionListeners(
            new List<IConnectionListener> { new TestWithConnectionFactoryCachedConnectionListener(createNotification, closedNotification) });

        var con1 = ccf.CreateConnection();
        VerifyConnectionIs(mockConnections[0].Object, con1);
        Assert.Single(ccf.AllocatedConnections);
        Assert.Empty(ccf.IdleConnections);

        Assert.NotNull(createNotification.Value);
        var val = createNotification.GetAndSet(null);
        Assert.Same(val, mockConnections[0].Object);

        var channel1 = con1.CreateChannel();
        VerifyChannelIs(mockChannels[0].Object, channel1);
        channel1.Close();

        // AMQP-358
        mockChannels[0].Verify(c => c.Close(), Times.Never);

        con1.Close();
        mockConnections[0].Verify(c => c.Close(), Times.Never);
        Assert.Single(ccf.AllocatedConnections);
        Assert.Single(ccf.IdleConnections);
        Assert.Null(closedNotification.Value);

        // Will retrieve same connection that was just put into cache, and reuse single channel from cache as well
        var con2 = ccf.CreateConnection();
        VerifyConnectionIs(mockConnections[0].Object, con2);
        var channel2 = con2.CreateChannel();
        VerifyChannelIs(mockChannels[0].Object, channel2);
        channel2.Close();
        mockChannels[0].Verify(c => c.Close(), Times.Never);
        con2.Close();
        mockConnections[0].Verify(c => c.Close(), Times.Never);
        Assert.Single(ccf.AllocatedConnections);
        Assert.Single(ccf.IdleConnections);
        Assert.Null(createNotification.Value);

        // Now check for multiple connections/channels
        con1 = ccf.CreateConnection();
        VerifyConnectionIs(mockConnections[0].Object, con1);
        con2 = ccf.CreateConnection();
        VerifyConnectionIs(mockConnections[1].Object, con2);
        channel1 = con1.CreateChannel();
        VerifyChannelIs(mockChannels[0].Object, channel1);
        channel2 = con2.CreateChannel();
        VerifyChannelIs(mockChannels[1].Object, channel2);
        Assert.Equal(2, ccf.AllocatedConnections.Count);
        Assert.Empty(ccf.IdleConnections);
        Assert.NotNull(createNotification.Value);
        val = createNotification.GetAndSet(null);
        Assert.Same(val, mockConnections[1].Object);

        // put mock1 in cache
        channel1.Close();
        mockChannels[1].Verify(c => c.Close(), Times.Never);
        con1.Close();
        mockConnections[0].Verify(c => c.Close(), Times.Never);
        Assert.Equal(2, ccf.AllocatedConnections.Count);
        Assert.Single(ccf.IdleConnections);
        Assert.Null(closedNotification.Value);

        var con3 = ccf.CreateConnection();
        Assert.Null(createNotification.Value);
        VerifyConnectionIs(mockConnections[0].Object, con3);
        var channel3 = con3.CreateChannel();
        VerifyChannelIs(mockChannels[0].Object, channel3);

        Assert.Equal(2, ccf.AllocatedConnections.Count);
        Assert.Empty(ccf.IdleConnections);

        channel2.Close();
        con2.Close();
        Assert.Equal(2, ccf.AllocatedConnections.Count);
        Assert.Single(ccf.IdleConnections);
        channel3.Close();
        con3.Close();
        Assert.Equal(2, ccf.AllocatedConnections.Count);
        Assert.Equal(2, ccf.IdleConnections.Count);
        Assert.Equal(1, ccf.CountOpenConnections());

        // Cache size is 1; con3 (mock1) should have been a real close.
        // con2 (mock2) should still be in the cache.
        mockConnections[0].Verify(c => c.Close(30_000));
        Assert.NotNull(closedNotification.Value);
        val = closedNotification.GetAndSet(null);
        Assert.Same(val, mockConnections[0].Object);
        mockChannels[1].Verify(c => c.Close(), Times.Never);
        mockConnections[1].Verify(c => c.Close(30_000), Times.Never);

        // verify(mockChannels.get(1), never()).close();
        VerifyConnectionIs(mockConnections[1].Object, ccf.IdleConnections.First.Value);

        // Now a closed cached connection
        mockConnections[1].Setup(c => c.IsOpen).Returns(false);
        mockChannels[1].Setup(c => c.IsOpen).Returns(false);
        con3 = ccf.CreateConnection();
        Assert.NotNull(closedNotification.Value);
        val = closedNotification.GetAndSet(null);
        Assert.Same(mockConnections[1].Object, val);
        VerifyConnectionIs(mockConnections[2].Object, con3);
        Assert.NotNull(createNotification.Value);
        val = createNotification.GetAndSet(null);
        Assert.Same(mockConnections[2].Object, val);
        Assert.Equal(2, ccf.AllocatedConnections.Count);
        Assert.Single(ccf.IdleConnections);
        Assert.Equal(1, ccf.CountOpenConnections());
        channel3 = con3.CreateChannel();
        VerifyChannelIs(mockChannels[2].Object, channel3);
        channel3.Close();
        con3.Close();
        Assert.Null(closedNotification.Value);
        Assert.Equal(2, ccf.AllocatedConnections.Count);
        Assert.Equal(2, ccf.IdleConnections.Count);
        Assert.Equal(1, ccf.CountOpenConnections());

        // Now a closed cached connection when creating a channel
        con3 = ccf.CreateConnection();
        VerifyConnectionIs(mockConnections[2].Object, con3);
        Assert.Null(createNotification.Value);
        Assert.Equal(2, ccf.AllocatedConnections.Count);
        Assert.Single(ccf.IdleConnections);
        mockConnections[2].Setup(c => c.IsOpen).Returns(false);
        channel3 = con3.CreateChannel();
        val = closedNotification.GetAndSet(null);
        Assert.NotNull(val);
        val = createNotification.GetAndSet(null);
        Assert.NotNull(val);
        VerifyChannelIs(mockChannels[3].Object, channel3);
        channel3.Close();
        con3.Close();
        Assert.Null(closedNotification.Value);
        Assert.Equal(2, ccf.AllocatedConnections.Count);
        Assert.Equal(2, ccf.IdleConnections.Count);
        Assert.Equal(1, ccf.CountOpenConnections());

        ccf.Destroy();
        Assert.NotNull(closedNotification.Value);
        mockConnections[3].Verify(c => c.Close(30_000));
    }

    [Fact]
    public void TestWithConnectionFactoryCachedConnectionAndChannels()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnections = new List<Mock<RC.IConnection>>();
        var mockChannels = new List<Mock<RC.IModel>>();

        var connectionNumber = new AtomicInteger();
        var channelNumber = new AtomicInteger();

        mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>()))
            .Callback(() =>
            {
                var connection = new Mock<RC.IConnection>();
                connection.Setup(c => c.CreateModel())
                    .Callback(() =>
                    {
                        var channel = new Mock<RC.IModel>();
                        channel.Setup(c => c.IsOpen).Returns(true);
                        var channelNum = channelNumber.IncrementAndGet();
                        channel.Setup(c => c.ToString()).Returns($"mockChannel{connectionNumber}:{channelNum}");
                        mockChannels.Add(channel);
                    })
                    .Returns(() => mockChannels[channelNumber.Value - 1].Object);
                var connectionNum = connectionNumber.IncrementAndGet();
                connection.Setup(c => c.ToString()).Returns($"mockConnection{connectionNum}");
                connection.Setup(c => c.IsOpen).Returns(true);
                mockConnections.Add(connection);
            })
            .Returns(() => mockConnections[connectionNumber.Value - 1].Object);

        var ccf = new CachingConnectionFactory(mockConnectionFactory.Object, false, CachingConnectionFactory.CachingMode.Connection)
        {
            ConnectionCacheSize = 2,
            ChannelCacheSize = 2
        };

        Assert.Empty(ccf.AllocatedConnections);
        Assert.Empty(ccf.IdleConnections);

        var createNotification = new AtomicReference<RC.IConnection>();
        var closedNotification = new AtomicReference<RC.IConnection>();
        ccf.SetConnectionListeners(
            new List<IConnectionListener> { new TestWithConnectionFactoryCachedConnectionListener(createNotification, closedNotification) });

        var con1 = ccf.CreateConnection();
        VerifyConnectionIs(mockConnections[0].Object, con1);
        Assert.Single(ccf.AllocatedConnections);
        Assert.Empty(ccf.IdleConnections);

        Assert.NotNull(createNotification.Value);
        var val = createNotification.GetAndSet(null);
        Assert.Same(val, mockConnections[0].Object);

        var channel1 = con1.CreateChannel();
        VerifyChannelIs(mockChannels[0].Object, channel1);
        channel1.Close();

        // AMQP-358
        mockChannels[0].Verify(c => c.Close(), Times.Never);

        con1.Close();
        mockConnections[0].Verify(c => c.Close(), Times.Never);
        Assert.Single(ccf.AllocatedConnections);
        Assert.Single(ccf.IdleConnections);
        var con1Proxy = con1 as CachingConnectionFactory.ChannelCachingConnectionProxy;
        Assert.Single(ccf.AllocatedConnectionNonTransactionalChannels[con1Proxy]);
        Assert.Null(closedNotification.Value);

        // Will retrieve same connection that was just put into cache, and reuse single channel from cache as well
        var con2 = ccf.CreateConnection();
        VerifyConnectionIs(mockConnections[0].Object, con2);
        var channel2 = con2.CreateChannel();
        VerifyChannelIs(mockChannels[0].Object, channel2);
        channel2.Close();
        mockChannels[0].Verify(c => c.Close(), Times.Never);
        con2.Close();
        mockConnections[0].Verify(c => c.Close(), Times.Never);
        Assert.Single(ccf.AllocatedConnections);
        Assert.Single(ccf.IdleConnections);
        Assert.Null(createNotification.Value);

        // Now check for multiple connections/channels
        con1 = ccf.CreateConnection();
        VerifyConnectionIs(mockConnections[0].Object, con1);
        con2 = ccf.CreateConnection();
        VerifyConnectionIs(mockConnections[1].Object, con2);
        channel1 = con1.CreateChannel();
        VerifyChannelIs(mockChannels[0].Object, channel1);
        channel2 = con2.CreateChannel();
        VerifyChannelIs(mockChannels[1].Object, channel2);
        Assert.Equal(2, ccf.AllocatedConnections.Count);
        Assert.Empty(ccf.IdleConnections);
        Assert.NotNull(createNotification.Value);
        val = createNotification.GetAndSet(null);
        Assert.Same(val, mockConnections[1].Object);

        // put mock1 in cache
        channel1.Close();
        mockChannels[1].Verify(c => c.Close(), Times.Never);
        con1.Close();
        mockConnections[0].Verify(c => c.Close(), Times.Never);
        Assert.Equal(2, ccf.AllocatedConnections.Count);
        Assert.Single(ccf.IdleConnections);
        Assert.Null(closedNotification.Value);

        var con3 = ccf.CreateConnection();
        Assert.Null(createNotification.Value);
        VerifyConnectionIs(mockConnections[0].Object, con3);
        var channel3 = con3.CreateChannel();
        VerifyChannelIs(mockChannels[0].Object, channel3);

        Assert.Equal(2, ccf.AllocatedConnections.Count);
        Assert.Empty(ccf.IdleConnections);

        channel2.Close();
        con2.Close();
        Assert.Equal(2, ccf.AllocatedConnections.Count);
        Assert.Single(ccf.IdleConnections);
        channel3.Close();
        con3.Close();
        Assert.Equal(2, ccf.AllocatedConnections.Count);
        Assert.Equal(2, ccf.IdleConnections.Count);
        con1Proxy = con1 as CachingConnectionFactory.ChannelCachingConnectionProxy;
        Assert.Single(ccf.AllocatedConnectionNonTransactionalChannels[con1Proxy]);
        var con2Proxy = con2 as CachingConnectionFactory.ChannelCachingConnectionProxy;
        Assert.Single(ccf.AllocatedConnectionNonTransactionalChannels[con2Proxy]);

        // Cache size is 1; con3 (mock1) should have been a real close.
        // con2 (mock2) should still be in the cache.
        mockConnections[0].Verify(c => c.Close(30_000), Times.Never);
        Assert.Null(closedNotification.Value);
        mockChannels[1].Verify(c => c.Close(), Times.Never);
        mockConnections[1].Verify(c => c.Close(30_000), Times.Never);

        // verify(mockChannels.get(1), never()).close();
        Assert.Equal(2, ccf.IdleConnections.Count);
        using var idleEnumerator = ccf.IdleConnections.GetEnumerator();
        Assert.True(idleEnumerator.MoveNext());
        VerifyConnectionIs(mockConnections[1].Object, idleEnumerator.Current);
        Assert.True(idleEnumerator.MoveNext());
        VerifyConnectionIs(mockConnections[0].Object, idleEnumerator.Current);

        // Now a closed cached connection
        mockConnections[1].Setup(c => c.IsOpen).Returns(false);
        con3 = ccf.CreateConnection();
        Assert.NotNull(closedNotification.Value);
        val = closedNotification.GetAndSet(null);
        Assert.Same(mockConnections[1].Object, val);
        VerifyConnectionIs(mockConnections[0].Object, con3);
        Assert.Null(createNotification.Value);
        Assert.Equal(2, ccf.AllocatedConnections.Count);
        Assert.Single(ccf.IdleConnections);
        channel3 = con3.CreateChannel();
        VerifyChannelIs(mockChannels[0].Object, channel3);
        channel3.Close();
        con3.Close();
        Assert.Null(closedNotification.Value);
        Assert.Equal(2, ccf.AllocatedConnections.Count);
        Assert.Equal(2, ccf.IdleConnections.Count);

        // Now a closed cached connection when creating a channel
        con3 = ccf.CreateConnection();
        VerifyConnectionIs(mockConnections[0].Object, con3);
        Assert.Null(createNotification.Value);
        Assert.Equal(2, ccf.AllocatedConnections.Count);
        Assert.Single(ccf.IdleConnections);
        mockConnections[0].Setup(c => c.IsOpen).Returns(false);
        channel3 = con3.CreateChannel();
        val = closedNotification.GetAndSet(null);
        Assert.NotNull(val);
        val = createNotification.GetAndSet(null);
        Assert.NotNull(val);

        VerifyChannelIs(mockChannels[2].Object, channel3);
        channel3.Close();
        con3.Close();
        Assert.Null(closedNotification.Value);
        Assert.Equal(2, ccf.AllocatedConnections.Count);
        Assert.Equal(2, ccf.IdleConnections.Count);

        var con4 = ccf.CreateConnection();
        Assert.Same(con3, con4);
        Assert.Single(ccf.IdleConnections);
        var channelA = con4.CreateChannel();
        var channelB = con4.CreateChannel();
        var channelC = con4.CreateChannel();
        channelA.Close();
        var con4Proxy = con4 as CachingConnectionFactory.ChannelCachingConnectionProxy;
        Assert.Single(ccf.AllocatedConnectionNonTransactionalChannels[con4Proxy]);
        channelB.Close();
        con4Proxy = con4 as CachingConnectionFactory.ChannelCachingConnectionProxy;
        Assert.Equal(2, ccf.AllocatedConnectionNonTransactionalChannels[con4Proxy].Count);
        channelC.Close();
        con4Proxy = con4 as CachingConnectionFactory.ChannelCachingConnectionProxy;
        Assert.Equal(2, ccf.AllocatedConnectionNonTransactionalChannels[con4Proxy].Count);

        ccf.Destroy();
        Assert.NotNull(closedNotification.Value);
        mockConnections[0].Verify(c => c.Close(30_000));
        mockConnections[1].Verify(c => c.Close(30_000));
        mockConnections[2].Verify(c => c.Close(30_000));
    }

    [Fact]
    public void TestWithConnectionFactoryCachedConnectionIdleAreClosed()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnections = new List<Mock<RC.IConnection>>();
        var mockChannels = new List<Mock<RC.IModel>>();

        var connectionNumber = new AtomicInteger();
        var channelNumber = new AtomicInteger();

        mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>()))
            .Callback(() =>
            {
                var connection = new Mock<RC.IConnection>();
                connection.Setup(c => c.CreateModel())
                    .Callback(() =>
                    {
                        var channel = new Mock<RC.IModel>();
                        channel.Setup(c => c.IsOpen).Returns(true);
                        var channelNum = channelNumber.IncrementAndGet();
                        channel.Setup(c => c.ToString()).Returns($"mockChannel{connectionNumber}:{channelNum}");
                        mockChannels.Add(channel);
                    })
                    .Returns(() => mockChannels[channelNumber.Value - 1].Object);
                var connectionNum = connectionNumber.IncrementAndGet();
                connection.Setup(c => c.ToString()).Returns($"mockConnection{connectionNum}");
                connection.Setup(c => c.IsOpen).Returns(true);
                mockConnections.Add(connection);
            })
            .Returns(() => mockConnections[connectionNumber.Value - 1].Object);

        var ccf = new CachingConnectionFactory(mockConnectionFactory.Object, false, CachingConnectionFactory.CachingMode.Connection)
        {
            ConnectionCacheSize = 5
        };

        Assert.Empty(ccf.AllocatedConnections);
        Assert.Empty(ccf.IdleConnections);

        var conn1 = ccf.CreateConnection();
        var conn2 = ccf.CreateConnection();
        var conn3 = ccf.CreateConnection();
        Assert.Equal(3, ccf.AllocatedConnections.Count);
        Assert.Empty(ccf.IdleConnections);
        conn1.Close();
        conn2.Close();
        conn3.Close();
        Assert.Equal(3, ccf.AllocatedConnections.Count);
        Assert.Equal(3, ccf.IdleConnections.Count);

        mockConnections[0].Setup(c => c.IsOpen).Returns(false);
        mockConnections[1].Setup(c => c.IsOpen).Returns(false);
        var conn4 = ccf.CreateConnection();
        Assert.Equal(3, ccf.AllocatedConnections.Count);
        Assert.Equal(2, ccf.IdleConnections.Count);
        Assert.Same(conn4, conn3);
        conn4.Close();
        Assert.Equal(3, ccf.AllocatedConnections.Count);
        Assert.Equal(3, ccf.IdleConnections.Count);
        Assert.Equal(1, ccf.CountOpenConnections());

        ccf.Destroy();
        Assert.Equal(3, ccf.AllocatedConnections.Count);
        Assert.Equal(3, ccf.IdleConnections.Count);
        Assert.Equal(0, ccf.CountOpenConnections());
    }

    [Fact]
    public void TestConsumerChannelPhysicallyClosedWhenNotIsOpen()
    {
        TestConsumerChannelPhysicallyClosedWhenNotIsOpenGuts(false);
    }

    [Fact]
    public void TestConsumerChannelWithPubConfPhysicallyClosedWhenNotIsOpen()
    {
        TestConsumerChannelPhysicallyClosedWhenNotIsOpenGuts(true);
    }

    [Fact(Skip = "Can't Mock sealed class: RC.ConnectionFactory")]
    public void SetAddressesEmpty()
    {
        var mock = new Mock<RC.ConnectionFactory>();
        var ccf = new CachingConnectionFactory(mock.Object)
        {
            Host = "abc"
        };
        ccf.SetAddresses(string.Empty);
        ccf.CreateConnection();
        mock.VerifyGet(f => f.AutomaticRecoveryEnabled);
        mock.VerifySet(f => f.HostName = "abc");
        mock.Verify(f => f.CreateConnection(It.IsAny<string>()));
    }

    [Fact(Skip = "Can't Mock sealed class: RC.ConnectionFactory")]
    public void SetAddressesOneHost()
    {
        var mock = new Mock<RC.ConnectionFactory>();
        IList<RC.AmqpTcpEndpoint> captured = null;
        mock.Setup(f => f.CreateConnection(It.IsAny<IList<RC.AmqpTcpEndpoint>>()))
            .Callback<IList<RC.AmqpTcpEndpoint>, string>((arg1, _) => captured = arg1);
        var ccf = new CachingConnectionFactory(mock.Object);
        ccf.SetAddresses("mq1");
        ccf.CreateConnection();
        mock.VerifyGet(f => f.AutomaticRecoveryEnabled);
        mock.Verify(f => f.CreateConnection(It.IsAny<IList<string>>(), It.IsAny<string>()));
        Assert.NotNull(captured);
        Assert.Equal("mq1", captured[0].HostName);
    }

    [Fact(Skip = "Can't Mock sealed class: RC.ConnectionFactory")]
    public void SetAddressesTwoHosts()
    {
        var mock = new Mock<RC.ConnectionFactory>();
        IList<RC.AmqpTcpEndpoint> captured = null;
        mock.Setup(f => f.CreateConnection(It.IsAny<IList<RC.AmqpTcpEndpoint>>()))
            .Callback<IList<RC.AmqpTcpEndpoint>, string>((arg1, _) => captured = arg1);
        mock.Setup(f => f.AutomaticRecoveryEnabled).Returns(true);
        var ccf = new CachingConnectionFactory(mock.Object);
        ccf.SetAddresses("mq1,mq2");
        ccf.CreateConnection();
        mock.VerifyGet(f => f.AutomaticRecoveryEnabled);
        mock.VerifySet(f => f.AutomaticRecoveryEnabled = false);
        mock.Verify(f => f.CreateConnection(It.IsAny<IList<string>>(), It.IsAny<string>()));
        Assert.NotNull(captured);
        Assert.Equal("mq1", captured[0].HostName);
        Assert.Equal("mq2", captured[1].HostName);
    }

    [Fact]
    public void SetUri()
    {
        var uri = new Uri("amqp://localhost:1234/%2f");
        var mock = new Mock<RC.IConnectionFactory>();
        var ccf = new CachingConnectionFactory(mock.Object)
        {
            Uri = uri
        };
        ccf.CreateConnection();
        mock.VerifySet(f => f.Uri = uri);
        mock.Verify(f => f.CreateConnection(It.IsAny<string>()));
    }

    [Fact]
    public void TestChannelCloseIdempotency()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnection = new Mock<RC.IConnection>();
        var mockChannel = new Mock<RC.IModel>();
        mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>())).Returns(mockConnection.Object);
        mockConnection.Setup(c => c.CreateModel()).Returns(mockChannel.Object);
        mockConnection.Setup(c => c.IsOpen).Returns(true);
        mockChannel.SetupSequence(c => c.IsOpen).Returns(true).Returns(false);

        var ccf = new CachingConnectionFactory(mockConnectionFactory.Object);
        var con = ccf.CreateConnection();
        var channel1 = con.CreateChannel();
        channel1.Close(); // should be ignored, and placed into channel cache.
        channel1.Close(); // physically closed, so remove from the cache.
        channel1.Close(); // physically closed and removed from the cache  before, so void "close".
        var channel2 = con.CreateChannel();
        Assert.NotSame(channel1, channel2);
    }

    [Fact]
    public void TestOrderlyShutDown()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnection = new Mock<RC.IConnection>();
        var mockChannel = new Mock<RC.IModel>();
        mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>())).Returns(mockConnection.Object);
        mockConnection.Setup(c => c.CreateModel()).Returns(mockChannel.Object);
        mockConnection.Setup(c => c.IsOpen).Returns(true);
        mockChannel.Setup(c => c.IsOpen).Returns(true);

        var ccf = new CachingConnectionFactory(mockConnectionFactory.Object)
        {
            PublisherConfirmType = CachingConnectionFactory.ConfirmType.Correlated
        };

        var pccMock = new Mock<IPublisherCallbackChannel>();
        pccMock.Setup(p => p.IsOpen).Returns(true);
        var asyncClosingLatch = new CountdownEvent(1);
        pccMock.Setup(p => p.WaitForConfirmsOrDie(It.IsAny<TimeSpan>()))
            .Callback(() => asyncClosingLatch.Signal());
        var closeLatch = new CountdownEvent(1);
        ccf.PublisherCallbackChannelFactory = new TestOrderlyShutdownPublisherCallbackChannelFactory(pccMock);
        pccMock.Setup(p => p.Close())
            .Callback(() =>
            {
                closeLatch.Signal();
            });
        var channel = ccf.CreateConnection().CreateChannel();
        Task.Run(() =>
        {
            RabbitUtils.SetPhysicalCloseRequired(channel, true);
            try
            {
                channel.Close();
            }
            catch (Exception)
            {
                // ignore
            }
        });
        Assert.True(asyncClosingLatch.Wait(TimeSpan.FromSeconds(10)));
        ccf.Destroy();
        Assert.True(closeLatch.Wait(TimeSpan.FromSeconds(10)));
    }

    [Fact]
    public void TestFirstConnectionDoesntWait()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnection = new Mock<RC.IConnection>();
        var mockChannel = new Mock<RC.IModel>();
        mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>())).Returns(mockConnection.Object);
        mockConnection.Setup(c => c.CreateModel()).Returns(mockChannel.Object);
        mockConnection.Setup(c => c.IsOpen).Returns(true);
        mockChannel.Setup(c => c.IsOpen).Returns(true);
        var ccf = new CachingConnectionFactory(mockConnectionFactory.Object, false, CachingConnectionFactory.CachingMode.Connection)
        {
            ChannelCheckoutTimeout = 60_000
        };
        var t1 = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        ccf.CreateConnection();
        Assert.True(DateTimeOffset.Now.ToUnixTimeMilliseconds() - t1 < 30_000);
    }

    [Fact]
    public void TestShuffle()
    {
        var captors = new List<IList<RC.AmqpTcpEndpoint>>();
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnection = new Mock<RC.IConnection>();
        var mockChannel = new Mock<RC.IModel>();
        mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<IList<RC.AmqpTcpEndpoint>>()))
            .Callback<IList<RC.AmqpTcpEndpoint>>(arg1 => captors.Add(arg1))
            .Returns(mockConnection.Object);
        mockConnection.Setup(c => c.CreateModel()).Returns(mockChannel.Object);
        mockConnection.Setup(c => c.IsOpen).Returns(true);
        mockChannel.Setup(c => c.IsOpen).Returns(true);
        var ccf = new CachingConnectionFactory(mockConnectionFactory.Object, false, CachingConnectionFactory.CachingMode.Connection);
        ccf.SetAddresses("host1:5672,host2:5672,host3:5672");
        ccf.ShuffleAddresses = true;
        for (var i = 0; i < 100; i++)
        {
            ccf.CreateConnection();
        }

        ccf.Destroy();
        var firstAddress = captors.SelectMany(e => e).Select(e => e.HostName).Distinct().ToList();
        firstAddress.Sort();
        Assert.Equal(3, firstAddress.Count);
        Assert.Equal("host1", firstAddress[0]);
        Assert.Equal("host2", firstAddress[1]);
        Assert.Equal("host3", firstAddress[2]);
    }

    [Fact]
    public void ConfirmsSimple()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var cf = new CachingConnectionFactory(mockConnectionFactory.Object)
        {
            PublisherConfirmType = CachingConnectionFactory.ConfirmType.None
        };
        Assert.False(cf.IsSimplePublisherConfirms);
        cf.PublisherConfirmType = CachingConnectionFactory.ConfirmType.Simple;
        Assert.True(cf.IsSimplePublisherConfirms);
        Assert.True(cf.PublisherConnectionFactory.IsSimplePublisherConfirms);
        cf.PublisherConfirmType = CachingConnectionFactory.ConfirmType.None;
        Assert.False(cf.IsSimplePublisherConfirms);
        Assert.False(cf.PublisherConnectionFactory.IsSimplePublisherConfirms);
    }

    [Fact]
    public void ConfirmsCorrelated()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var cf = new CachingConnectionFactory(mockConnectionFactory.Object)
        {
            PublisherConfirmType = CachingConnectionFactory.ConfirmType.None
        };
        Assert.False(cf.IsPublisherConfirms);
        cf.PublisherConfirmType = CachingConnectionFactory.ConfirmType.Correlated;
        Assert.True(cf.IsPublisherConfirms);
        Assert.True(cf.PublisherConnectionFactory.IsPublisherConfirms);
        cf.PublisherConfirmType = CachingConnectionFactory.ConfirmType.None;
        Assert.False(cf.IsPublisherConfirms);
        Assert.False(cf.PublisherConnectionFactory.IsPublisherConfirms);
    }

    protected override AbstractConnectionFactory CreateConnectionFactory(RC.IConnectionFactory connectionFactory, ILoggerFactory loggerFactory = null)
    {
        return new CachingConnectionFactory(connectionFactory, loggerFactory);
    }

    private void TestConsumerChannelPhysicallyClosedWhenNotIsOpenGuts(bool confirms)
    {
        try
        {
            var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
            var mockConnection = new Mock<RC.IConnection>();
            var mockChannel1 = new Mock<RC.IModel>();

            mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>())).Returns(mockConnection.Object);
            mockConnection.Setup(c => c.CreateModel()).Returns(mockChannel1.Object);
            mockConnection.Setup(c => c.IsOpen).Returns(true);
            mockChannel1.Setup(c => c.IsOpen).Returns(true);

            var ccf = new CachingConnectionFactory(mockConnectionFactory.Object);
            if (confirms)
            {
                ccf.PublisherConfirmType = CachingConnectionFactory.ConfirmType.Correlated;
            }

            var con = ccf.CreateConnection();
            var channel = con.CreateChannel();
            RabbitUtils.SetPhysicalCloseRequired(channel, true);
            mockChannel1.Setup(c => c.IsOpen).Returns(true);
            var physicalCloseLatch = new CountdownEvent(1);

            mockChannel1.Setup(c => c.Close())
                .Callback(() => physicalCloseLatch.Signal());
            channel.Close();
            RabbitUtils.SetPhysicalCloseRequired(channel, false);
            con.Close(); // should be ignored

            Assert.True(physicalCloseLatch.Wait(TimeSpan.FromSeconds(10)));
        }
        catch (Exception)
        {
            // Ignore
        }
    }

    private void VerifyChannelIs(RC.IModel mockChannel, RC.IModel channel)
    {
        var proxy = (IChannelProxy)channel;
        Assert.Same(proxy.TargetChannel, mockChannel);
    }

    private void VerifyConnectionIs(RC.IConnection mockConnection, object con)
    {
        var asProxy = con as CachingConnectionFactory.ChannelCachingConnectionProxy;
        var conDelegate = asProxy.TargetConnection.Connection;
        Assert.Same(mockConnection, conDelegate);
    }

    private async Task TestCheckoutLimitWithPublisherConfirms(bool physicalClose)
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnection = new Mock<RC.IConnection>();
        var mockChannel = new Mock<RC.IModel>();
        var mockProperties = new Mock<RC.IBasicProperties>();
        mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>()))
            .Returns(mockConnection.Object);
        mockConnection.Setup(c => c.CreateModel()).Returns(mockChannel.Object);
        mockConnection.Setup(c => c.IsOpen).Returns(true);
        mockChannel.Setup(c => c.IsOpen).Returns(true);
        mockChannel.Setup(c => c.CreateBasicProperties()).Returns(mockProperties.Object);
        var confirmsLatch = new CountdownEvent(1);
        mockChannel.Setup(c => c.WaitForConfirmsOrDie(It.IsAny<TimeSpan>()))
            .Callback(() =>
            {
                confirmsLatch.Wait(TimeSpan.FromSeconds(10));
            });

        mockChannel.SetupAdd(c => c.BasicAcks += It.IsAny<EventHandler<BasicAckEventArgs>>());

        mockChannel.Setup(c => c.NextPublishSeqNo).Returns(1);
        var ccf = new CachingConnectionFactory(mockConnectionFactory.Object)
        {
            ChannelCacheSize = 1,
            ChannelCheckoutTimeout = 1,
            PublisherConfirmType = CachingConnectionFactory.ConfirmType.Correlated
        };

        var con = ccf.CreateConnection();
        var rabbitTemplate = new RabbitTemplate(ccf);
        if (physicalClose)
        {
            var channel1 = con.CreateChannel();
            RabbitUtils.SetPhysicalCloseRequired(channel1, physicalClose);
            channel1.Close();
        }
        else
        {
            rabbitTemplate.ConvertAndSend("foo", "bar"); // pending confirm
        }

        Assert.Throws<RabbitTimeoutException>(() => con.CreateChannel());
        var n = 0;
        if (physicalClose)
        {
            confirmsLatch.Signal();
            RC.IModel channel2 = null;
            while (channel2 == null && n++ < 100)
            {
                try
                {
                    channel2 = con.CreateChannel();
                }
                catch (Exception)
                {
                    await Task.Delay(100);
                }
            }

            Assert.NotNull(channel2);
        }
        else
        {
            mockChannel.Raise(m => m.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = 1, Multiple = false });
            var ok = false;
            while (!ok && n++ < 100)
            {
                try
                {
                    rabbitTemplate.ConvertAndSend("foo", "bar");
                    ok = true;
                }
                catch (Exception)
                {
                    await Task.Delay(100);
                }
            }

            Assert.True(ok);
        }
    }

    private void TestCheckoutsWithRefreshedConnectionGuts(CachingConnectionFactory.CachingMode mode)
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnection1 = new Mock<RC.IConnection>();
        var mockConnection2 = new Mock<RC.IConnection>();
        var mockChannel1 = new Mock<RC.IModel>();
        var mockChanel2 = new Mock<RC.IModel>();
        var mockChanel3 = new Mock<RC.IModel>();
        var mockChanel4 = new Mock<RC.IModel>();
        mockConnectionFactory.SetupSequence(f => f.CreateConnection(It.IsAny<string>()))
            .Returns(mockConnection1.Object)
            .Returns(mockConnection2.Object);
        mockConnection1.SetupSequence(c => c.CreateModel())
            .Returns(mockChannel1.Object)
            .Returns(mockChanel2.Object);
        mockConnection1.Setup(c => c.IsOpen).Returns(true);
        mockConnection2.SetupSequence(c => c.CreateModel())
            .Returns(mockChanel3.Object)
            .Returns(mockChanel4.Object);
        mockConnection2.Setup(c => c.IsOpen).Returns(true);

        mockChannel1.Setup(c => c.IsOpen).Returns(true);
        mockChanel2.Setup(c => c.IsOpen).Returns(true);
        mockChanel3.Setup(c => c.IsOpen).Returns(true);
        mockChanel4.Setup(c => c.IsOpen).Returns(true);

        var ccf = new CachingConnectionFactory(mockConnectionFactory.Object, false, mode)
        {
            ChannelCacheSize = 2,
            ChannelCheckoutTimeout = 10
        };

        ccf.AddConnectionListener(new TestCheckoutsWithRefreshedConnectionGutsListener());
        var con = ccf.CreateConnection();
        var channel1 = con.CreateChannel();

        Assert.Single(ccf.CheckoutPermits.Values);
        var slim = ccf.CheckoutPermits.Values.Single();
        Assert.Equal(1, slim.CurrentCount);

        channel1.Close();
        con.Close();

        Assert.Equal(2, slim.CurrentCount);

        mockConnection1.Setup(c => c.IsOpen).Returns(false);
        mockChannel1.Setup(c => c.IsOpen).Returns(false);
        mockChanel2.Setup(c => c.IsOpen).Returns(false);

        con.CreateChannel().Close();
        con = ccf.CreateConnection();
        con.CreateChannel().Close();
        con.CreateChannel().Close();
        con.CreateChannel().Close();
        con.CreateChannel().Close();
        con.CreateChannel().Close();

        mockConnection1.Verify(c => c.CreateModel(), Times.Once);
        mockConnection2.Verify(c => c.CreateModel(), Times.Exactly(2));

        con.Close();
        mockConnection2.Verify(c => c.Close(), Times.Never);

        Assert.Equal(2, slim.CurrentCount);

        ccf.Destroy();

        Assert.Equal(2, slim.CurrentCount);
    }

    private sealed class TestOrderlyShutdownPublisherCallbackChannelFactory : IPublisherCallbackChannelFactory
    {
        private readonly Mock<IPublisherCallbackChannel> _pccMock;

        public TestOrderlyShutdownPublisherCallbackChannelFactory(Mock<IPublisherCallbackChannel> pccMock)
        {
            _pccMock = pccMock;
        }

        public IPublisherCallbackChannel CreateChannel(RC.IModel channel)
        {
            return _pccMock.Object;
        }
    }

    private sealed class TestWithConnectionFactoryCachedConnectionListener : IConnectionListener
    {
        private readonly AtomicReference<RC.IConnection> _createNotification;
        private readonly AtomicReference<RC.IConnection> _closedNotification;

        public TestWithConnectionFactoryCachedConnectionListener(AtomicReference<RC.IConnection> createNotification, AtomicReference<RC.IConnection> closedNotification)
        {
            _createNotification = createNotification;
            _closedNotification = closedNotification;
        }

        public void OnClose(IConnection connection)
        {
            Assert.Null(_closedNotification.Value);
            var asProxy = connection as CachingConnectionFactory.ChannelCachingConnectionProxy;
            var conDelegate = asProxy.TargetConnection.Connection;
            _closedNotification.Value = conDelegate;
        }

        public void OnCreate(IConnection connection)
        {
            Assert.Null(_createNotification.Value);
            var asProxy = connection as CachingConnectionFactory.ChannelCachingConnectionProxy;
            var conDelegate = asProxy.TargetConnection.Connection;
            _createNotification.Value = conDelegate;
        }

        public void OnShutDown(RC.ShutdownEventArgs args)
        {
        }
    }

    private sealed class TestWithConnectionListenerListener : IConnectionListener
    {
        private readonly AtomicReference<IConnection> _created;
        private readonly AtomicReference<IConnection> _closed;
        private readonly AtomicInteger _timesClosed;

        public TestWithConnectionListenerListener(AtomicReference<IConnection> created, AtomicReference<IConnection> closed, AtomicInteger timesClosed)
        {
            _created = created;
            _closed = closed;
            _timesClosed = timesClosed;
        }

        public void OnClose(IConnection connection)
        {
            _closed.Value = connection;
            _timesClosed.GetAndIncrement();
        }

        public void OnCreate(IConnection connection)
        {
            _created.Value = connection;
        }

        public void OnShutDown(RC.ShutdownEventArgs args)
        {
        }
    }

    private sealed class TestWithChannelListenerListener : IChannelListener
    {
        private readonly AtomicInteger _atomicInteger;

        public TestWithChannelListenerListener(AtomicInteger atomicInteger)
        {
            _atomicInteger = atomicInteger;
        }

        public void OnCreate(RC.IModel channel, bool transactional)
        {
            _atomicInteger.IncrementAndGet();
        }

        public void OnShutDown(RC.ShutdownEventArgs args)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class TestCheckoutsWithRefreshedConnectionGutsListener : IConnectionListener
    {
        public void OnClose(IConnection connection)
        {
        }

        public void OnCreate(IConnection connection)
        {
            connection.CreateChannel().Close();
        }

        public void OnShutDown(RC.ShutdownEventArgs args)
        {
        }
    }
}
