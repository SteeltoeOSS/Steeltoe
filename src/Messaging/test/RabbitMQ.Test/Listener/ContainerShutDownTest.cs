// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using RabbitMQ.Client.Impl;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.RabbitMQ.Configuration;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Listener;
using Xunit;
using static Steeltoe.Messaging.RabbitMQ.Connection.CachingConnectionFactory;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Test.Listener;

[Trait("Category", "Integration")]
public class ContainerShutDownTest : AbstractTest
{
    [Fact]
    public void TestUninterruptibleListenerDmlc()
    {
        using var cf = new CachingConnectionFactory("localhost");
        var admin = new RabbitAdmin(cf);
        admin.DeclareQueue(new Queue("test.shutdown"));

        var container = new DirectMessageListenerContainer(null, cf)
        {
            ShutdownTimeout = 500
        };

        var testEnded = new CountdownEvent(1);
        SessionManager channels = null;

        try
        {
            container.SetQueueNames("test.shutdown");
            var latch = new CountdownEvent(1);

            var listener = new TestListener(latch, testEnded);
            container.MessageListener = listener;
            var connection = cf.CreateConnection() as ChannelCachingConnectionProxy;

            FieldInfo field =
                typeof(global::RabbitMQ.Client.Framing.Impl.Connection).GetField("m_sessionManager", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(field);
            channels = (SessionManager)field.GetValue(connection.Target.Connection);
            Assert.NotNull(channels);

            container.StartAsync();
            Assert.True(container.StartedLatch.Wait(TimeSpan.FromSeconds(10)));

            var template = new RabbitTemplate(cf);

            template.Execute(c =>
            {
                RC.IBasicProperties properties = c.CreateBasicProperties();
                byte[] bytes = EncodingUtils.GetDefaultEncoding().GetBytes("foo");
                c.BasicPublish(string.Empty, "test.shutdown", false, properties, bytes);
                RabbitUtils.SetPhysicalCloseRequired(c, false);
            });

            Assert.True(latch.Wait(TimeSpan.FromSeconds(30)));
            Assert.Equal(2, channels.Count);
        }
        finally
        {
            container.StopAsync();

            if (channels != null)
            {
                Assert.Equal(1, channels.Count);
            }

            container.Dispose();

            testEnded.Signal();
            admin.DeleteQueue("test.shutdown");
        }
    }

    private sealed class TestListener : IMessageListener
    {
        private readonly CountdownEvent _latch;
        private readonly CountdownEvent _testEnded;

        public AcknowledgeMode ContainerAckMode { get; set; }

        public TestListener(CountdownEvent latch, CountdownEvent testEnded)
        {
            _latch = latch;
            _testEnded = testEnded;
        }

        public void OnMessage(IMessage message)
        {
            _latch.Signal();
            _testEnded.Wait(TimeSpan.FromSeconds(30));
        }

        public void OnMessageBatch(List<IMessage> messages)
        {
        }
    }
}
