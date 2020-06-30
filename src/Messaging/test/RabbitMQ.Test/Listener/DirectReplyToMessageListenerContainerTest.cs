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

using RabbitMQ.Client;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Rabbit.Connection;
using Steeltoe.Messaging.Rabbit.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Messaging.Rabbit.Listener
{
    public class DirectReplyToMessageListenerContainerTest : IDisposable
    {
        public const string TEST_RELEASE_CONSUMER_Q = "test.release.consumer";

        private ITestOutputHelper _output;

        public DirectReplyToMessageListenerContainerTest(ITestOutputHelper output)
        {
            var adminCf = new CachingConnectionFactory("localhost");
            var admin = new RabbitAdmin(adminCf);
            admin.DeclareQueue(new Config.Queue(TEST_RELEASE_CONSUMER_Q));
            _output = output;
        }

        public void Dispose()
        {
            var adminCf = new CachingConnectionFactory("localhost");
            var admin = new RabbitAdmin(adminCf);
            admin.DeleteQueue(TEST_RELEASE_CONSUMER_Q);
            adminCf.Dispose();
        }

        [Fact]
        public async Task TestReleaseConsumerRace()
        {
            var connectionFactory = new CachingConnectionFactory("localhost");
            var container = new DirectReplyToMessageListenerContainer(null, connectionFactory);

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
            channel1.Channel.BasicPublish(string.Empty, TEST_RELEASE_CONSUMER_Q, props, foobytes);
            var replyChannel = connectionFactory.CreateConnection().CreateChannel(false);
            var request = replyChannel.BasicGet(TEST_RELEASE_CONSUMER_Q, true);
            var n = 0;
            while (n++ < 100 && request == null)
            {
                Thread.Sleep(100);
                request = replyChannel.BasicGet(TEST_RELEASE_CONSUMER_Q, true);
            }

            Assert.NotNull(request);
            props = channel1.Channel.CreateBasicProperties();
            replyChannel.BasicPublish(string.Empty, request.BasicProperties.ReplyTo, props, barbytes);
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

        private class MockChannelAwareMessageListener : IChannelAwareMessageListener
        {
            public IChannelAwareMessageListener MessageListener;
            public CountdownEvent Latch;

            public MockChannelAwareMessageListener(IMessageListener messageListener, CountdownEvent latch)
            {
                MessageListener = messageListener as IChannelAwareMessageListener;
                Latch = latch;
            }

            public AcknowledgeMode ContainerAckMode { get; set; }

            public void OnMessage(IMessage message, IModel channel)
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

            public void OnMessageBatch(List<IMessage> messages, IModel channel)
            {
            }

            public void OnMessageBatch(List<IMessage> messages)
            {
            }
        }

        private class EmptyListener : IMessageListener
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
}
