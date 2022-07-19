// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using RabbitMQ.Client.Impl;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Xunit;
using static Steeltoe.Messaging.RabbitMQ.Connection.CachingConnectionFactory;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Listener;

[Trait("Category", "Integration")]
public class ContainerShutDownTest : AbstractTest
{
    [Fact]
    public void TestUninterruptibleListenerDMLC()
    {
        var cf = new CachingConnectionFactory("localhost");
        var admin = new RabbitAdmin(cf);
        admin.DeclareQueue(new Config.Queue("test.shutdown"));

        var container = new DirectMessageListenerContainer(null, cf)
        {
            ShutdownTimeout = 500
        };
        container.SetQueueNames("test.shutdown");
        var latch = new CountdownEvent(1);
        var testEnded = new CountdownEvent(1);
        var listener = new TestListener(latch, testEnded);
        container.MessageListener = listener;
        var connection = cf.CreateConnection() as ChannelCachingConnectionProxy;

        // var channels = TestUtils.getPropertyValue(connection, "target.delegate._channelManager._channelMap");
        var field = typeof(RC.Framing.Impl.Connection)
            .GetField("m_sessionManager", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        var channels = (SessionManager)field.GetValue(connection.Target.Connection);
        Assert.NotNull(channels);

        container.Start();
        Assert.True(container._startedLatch.Wait(TimeSpan.FromSeconds(10)));

        try
        {
            var template = new RabbitTemplate(cf);
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
            container.Dispose();

            testEnded.Signal();
            admin.DeleteQueue("test.shutdown");
            cf.Dispose();
        }
    }

    private sealed class TestListener : IMessageListener
    {
        private readonly CountdownEvent _latch;
        private readonly CountdownEvent _testEnded;

        public TestListener(CountdownEvent latch, CountdownEvent testEnded)
        {
            _latch = latch;
            _testEnded = testEnded;
        }

        public AcknowledgeMode ContainerAckMode { get; set; }

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
