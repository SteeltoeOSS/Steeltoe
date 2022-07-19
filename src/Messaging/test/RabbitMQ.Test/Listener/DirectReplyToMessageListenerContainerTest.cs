// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Listener;

[Trait("Category", "Integration")]
public sealed class DirectReplyToMessageListenerContainerTest : IDisposable
{
    private const string TEST_RELEASE_CONSUMER_Q = "test.release.consumer";
    private readonly CachingConnectionFactory _adminCf;
    private readonly RabbitAdmin _admin;

    public DirectReplyToMessageListenerContainerTest()
    {
        _adminCf = new CachingConnectionFactory("localhost");
        _admin = new RabbitAdmin(_adminCf);
        _admin.DeclareQueue(new Config.Queue(TEST_RELEASE_CONSUMER_Q));
    }

    public void Dispose()
    {
        _admin.DeleteQueue(TEST_RELEASE_CONSUMER_Q);
        _adminCf.Dispose();
    }

    [Fact]
    public async Task TestReleaseConsumerRace()
    {
        using var connectionFactory = new CachingConnectionFactory("localhost");
        using var container = new DirectReplyToMessageListenerContainer(null, connectionFactory);

        var latch = new CountdownEvent(1);
        container.MessageListener = new EmptyListener();
        var mockMessageListener = new MockChannelAwareMessageListener(container.MessageListener, latch);
        container.SetChannelAwareMessageListener(mockMessageListener);

        var foobytes = EncodingUtils.GetDefaultEncoding().GetBytes("foo");
        var barbytes = EncodingUtils.GetDefaultEncoding().GetBytes("bar");
        await container.Start();
        Assert.True(container._startedLatch.Wait(TimeSpan.FromSeconds(10)));

        var channel1 = container.GetChannelHolder();
        var props = channel1.Channel.CreateBasicProperties();
        props.ReplyTo = Address.AMQ_RABBITMQ_REPLY_TO;
        RC.IModelExensions.BasicPublish(channel1.Channel, string.Empty, TEST_RELEASE_CONSUMER_Q, props, foobytes);
        var replyChannel = connectionFactory.CreateConnection().CreateChannel();
        var request = replyChannel.BasicGet(TEST_RELEASE_CONSUMER_Q, true);
        var n = 0;
        while (n++ < 100 && request == null)
        {
            Thread.Sleep(100);
            request = replyChannel.BasicGet(TEST_RELEASE_CONSUMER_Q, true);
        }

        Assert.NotNull(request);
        props = channel1.Channel.CreateBasicProperties();
        RC.IModelExensions.BasicPublish(replyChannel, string.Empty, request.BasicProperties.ReplyTo, props, barbytes);
        replyChannel.Close();
        Assert.True(latch.Wait(TimeSpan.FromSeconds(10)));

        var channel2 = container.GetChannelHolder();
        Assert.Same(channel1.Channel, channel2.Channel);
        container.ReleaseConsumerFor(channel1, false, null); // simulate race for future timeout/cancel and onMessage()
        var inUse = container._inUseConsumerChannels;
        Assert.Single(inUse);
        container.ReleaseConsumerFor(channel2, false, null);
        Assert.Empty(inUse);
        await container.Stop();
        connectionFactory.Destroy();
    }

    private sealed class MockChannelAwareMessageListener : IChannelAwareMessageListener
    {
        public IChannelAwareMessageListener MessageListener;
        public CountdownEvent Latch;

        public MockChannelAwareMessageListener(IMessageListener messageListener, CountdownEvent latch)
        {
            MessageListener = messageListener as IChannelAwareMessageListener;
            Latch = latch;
        }

        public AcknowledgeMode ContainerAckMode { get; set; }

        public void OnMessage(IMessage message, RC.IModel channel)
        {
            try
            {
                MessageListener.OnMessage(message, channel);
            }
            finally
            {
                Latch.Signal();
            }
        }

        public void OnMessage(IMessage message)
        {
        }

        public void OnMessageBatch(List<IMessage> messages, RC.IModel channel)
        {
        }

        public void OnMessageBatch(List<IMessage> messages)
        {
        }
    }

    private sealed class EmptyListener : IMessageListener
    {
        public AcknowledgeMode ContainerAckMode { get; set; }

        public void OnMessage(IMessage message)
        {
        }

        public void OnMessageBatch(List<IMessage> messages)
        {
        }
    }
}
