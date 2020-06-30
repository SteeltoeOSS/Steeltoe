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

using RabbitMQ.Client.Impl;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Rabbit.Connection;
using Steeltoe.Messaging.Rabbit.Core;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Xunit;
using static Steeltoe.Messaging.Rabbit.Connection.CachingConnectionFactory;

namespace Steeltoe.Messaging.Rabbit.Listener
{
    public class ContainerShutDownTest : AbstractTest
    {
        [Fact]
        public void TestUninterruptibleListenerDMLC()
        {
            var cf = new CachingConnectionFactory("localhost");
            var admin = new RabbitAdmin(cf);
            admin.DeclareQueue(new Config.Queue("test.shutdown"));

            DirectMessageListenerContainer container = new DirectMessageListenerContainer(null, cf);
            container.ShutdownTimeout = 500;
            container.SetQueueNames("test.shutdown");
            var latch = new CountdownEvent(1);
            var testEnded = new CountdownEvent(1);
            var listener = new TestListener(latch, testEnded);
            container.MessageListener = listener;
            var connection = cf.CreateConnection() as ChannelCachingConnectionProxy;

            // var channels = TestUtils.getPropertyValue(connection, "target.delegate._channelManager._channelMap");
            var field = typeof(RabbitMQ.Client.Framing.Impl.Connection)
                .GetField("m_sessionManager", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            var channels = (SessionManager)field.GetValue(connection.Target.Connection);
            Assert.NotNull(channels);

            container.Start();
            Assert.True(container._startedLatch.Wait(TimeSpan.FromSeconds(10)));

            try
            {
                RabbitTemplate template = new RabbitTemplate(cf);
                template.Execute(c =>
                {
                    var properties = c.CreateBasicProperties();
                    var bytes = EncodingUtils.GetDefaultEncoding().GetBytes("foo");
                    c.BasicPublish(string.Empty, "test.shutdown", false, properties, bytes);
                    RabbitUtils.SetPhysicalCloseRequired(c, false);
                });
                Assert.True(latch.Wait(TimeSpan.FromSeconds(30)));
                Assert.Equal(2, channels.Count);
            }
            finally
            {
                container.Stop();
                Assert.Equal(1, channels.Count);

                cf.Destroy();
                testEnded.Signal();
                admin.DeleteQueue("test.shutdown");
            }
        }

        private class TestListener : IMessageListener
        {
            private CountdownEvent latch;
            private CountdownEvent testEnded;

            public TestListener(CountdownEvent latch, CountdownEvent testEnded)
            {
                this.latch = latch;
                this.testEnded = testEnded;
            }

            public AcknowledgeMode ContainerAckMode { get; set; }

            public void OnMessage(IMessage message)
            {
                try
                {
                    latch.Signal();
                    testEnded.Wait(TimeSpan.FromSeconds(30));
                }
                catch (Exception)
                {
                }
            }

            public void OnMessageBatch(List<IMessage> messages)
            {
            }
        }
    }
}
