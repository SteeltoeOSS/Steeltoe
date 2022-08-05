// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Xunit;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Listener;

[Trait("Category", "Integration")]
public sealed class DirectReplyToMessageListenerContainerTest : IDisposable
{
    private const string TestReleaseConsumerQ = "test.release.consumer";
    private readonly CachingConnectionFactory _adminCf;
    private readonly RabbitAdmin _admin;

    public DirectReplyToMessageListenerContainerTest()
    {
        _adminCf = new CachingConnectionFactory("localhost");
        _admin = new RabbitAdmin(_adminCf);
        _admin.DeclareQueue(new Queue(TestReleaseConsumerQ));
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

        byte[] fooBytes = EncodingUtils.GetDefaultEncoding().GetBytes("foo");
        byte[] barBytes = EncodingUtils.GetDefaultEncoding().GetBytes("bar");
        await container.StartAsync();
        Assert.True(container.StartedLatch.Wait(TimeSpan.FromSeconds(10)));

        DirectReplyToMessageListenerContainer.ChannelHolder channel1 = container.GetChannelHolder();
        RC.IBasicProperties props = channel1.Channel.CreateBasicProperties();
        props.ReplyTo = Address.AmqRabbitMQReplyTo;
        RC.IModelExensions.BasicPublish(channel1.Channel, string.Empty, TestReleaseConsumerQ, props, fooBytes);
        RC.IModel replyChannel = connectionFactory.CreateConnection().CreateChannel();
        RC.BasicGetResult request = replyChannel.BasicGet(TestReleaseConsumerQ, true);
        int n = 0;

        while (n++ < 100 && request == null)
        {
            Thread.Sleep(100);
            request = replyChannel.BasicGet(TestReleaseConsumerQ, true);
        }

        Assert.NotNull(request);
        props = channel1.Channel.CreateBasicProperties();
        RC.IModelExensions.BasicPublish(replyChannel, string.Empty, request.BasicProperties.ReplyTo, props, barBytes);
        replyChannel.Close();
        Assert.True(latch.Wait(TimeSpan.FromSeconds(10)));

        DirectReplyToMessageListenerContainer.ChannelHolder channel2 = container.GetChannelHolder();
        Assert.Same(channel1.Channel, channel2.Channel);
        container.ReleaseConsumerFor(channel1, false, null); // simulate race for future timeout/cancel and onMessage()
        ConcurrentDictionary<RC.IModel, DirectMessageListenerContainer.SimpleConsumer> inUse = container.InUseConsumerChannels;
        Assert.Single(inUse);
        container.ReleaseConsumerFor(channel2, false, null);
        Assert.Empty(inUse);
        await container.StopAsync();
        connectionFactory.Destroy();
    }

    public void Dispose()
    {
        _admin.DeleteQueue(TestReleaseConsumerQ);
        _adminCf.Dispose();
    }

    private sealed class MockChannelAwareMessageListener : IChannelAwareMessageListener
    {
        public readonly IChannelAwareMessageListener MessageListener;
        public readonly CountdownEvent Latch;

        public AcknowledgeMode ContainerAckMode { get; set; }

        public MockChannelAwareMessageListener(IMessageListener messageListener, CountdownEvent latch)
        {
            MessageListener = messageListener as IChannelAwareMessageListener;
            Latch = latch;
        }

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
