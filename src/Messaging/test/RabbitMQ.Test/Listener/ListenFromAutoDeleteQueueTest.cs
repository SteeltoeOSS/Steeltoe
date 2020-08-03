using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Transaction;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Rabbit.Config;
using Steeltoe.Messaging.Rabbit.Connection;
using Steeltoe.Messaging.Rabbit.Core;
using Steeltoe.Messaging.Rabbit.Listener.Adapters;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Messaging.Rabbit.Listener
{
    public class ListenFromAutoDeleteQueueTest
    {
        private DirectMessageListenerContainer listenerContainer1;
        private DirectMessageListenerContainer listenerContainer2;
        private DirectMessageListenerContainer listenerContainer3;
        private DirectMessageListenerContainer listenerContainer4;

        private Queue expiringQueue;
        private Connection.IConnectionFactory connectionFactory;
        private AppendingListener listener;
        private TestAdmin containerAdmin;

        public const string Exch1 = "testContainerWithAutoDeleteQueues";
        public const string Exch2 = "otherExchange";
        public const string Q1 = "anon";
        public const string Q2 = "anon2";
        public const string Q3 = "otherAnon";

        public ListenFromAutoDeleteQueueTest()
        {
            connectionFactory = new CachingConnectionFactory("localhost")
            {
                IsPublisherReturns = true
            };

            // Container Admin
            containerAdmin = new TestAdmin(connectionFactory);

            // Exchange
            var directExchange = new DirectExchange("testContainerWithAutoDeleteQueues", true, true);

            listenerContainer1 = new DirectMessageListenerContainer(null, connectionFactory, "container1");
            listenerContainer1.ConsumersPerQueue = 2;
            listenerContainer1.AddQueueNames(Q1, Q2);

            // Listener
            listener = new AppendingListener();
            var adapter = new MessageListenerAdapter(null, listener);
            listenerContainer1.MessageListener = adapter;
            listenerContainer1.Start();
            listenerContainer1._startedLatch.Wait(TimeSpan.FromSeconds(10));

            // Conditional declarations
            containerAdmin.DeclareQueue(new Queue(Q3));
            var otherExchange = new DirectExchange(Exch2, true, true);
            otherExchange.DeclaringAdmins = new List<object>() { containerAdmin };

            listenerContainer2 = new DirectMessageListenerContainer(null, connectionFactory, "container2");
            listenerContainer2.IsAutoStartup = false;
            listenerContainer2.ShutdownTimeout = 50;
            listenerContainer2.AddQueueNames(Q3);
            listenerContainer2.MessageListener = adapter;

            expiringQueue = new Queue(Guid.NewGuid().ToString(), true, false, false, new Dictionary<string, object>() { { "x-expires", 100 } });

            listenerContainer3 = new DirectMessageListenerContainer(null, connectionFactory, "container3");
            listenerContainer3.IsAutoStartup = false;
            listenerContainer3.ShutdownTimeout = 50;
            listenerContainer3.AddQueueNames(expiringQueue.QueueName);
            listenerContainer3.MessageListener = adapter;

            listenerContainer4 = new DirectMessageListenerContainer(null, connectionFactory, "container4");

            listenerContainer4.IsAutoStartup = false;
            listenerContainer4.ShutdownTimeout = 50;
            listenerContainer4.AddQueueNames(Q2);
            listenerContainer4.MessageListener = adapter;
            listenerContainer4.AutoDeclare = false;

        }

        [Fact]
        public void TestStopStart()
        {
            var rabbitTemplate = new RabbitTemplate(connectionFactory);
            rabbitTemplate.ConvertAndSend(Exch1, Q1, "foo");
            listener.Latch.Wait(TimeSpan.FromSeconds(10));
            Assert.True(listener.Queue.Count > 0);
            listenerContainer1.Stop();
            listenerContainer1.Start();
            listenerContainer1._startedLatch.Wait(TimeSpan.FromSeconds(10));
            rabbitTemplate.ConvertAndSend(Exch1, Q1, "foo");
            listener.Latch.Wait(TimeSpan.FromSeconds(10));
            Assert.True(listener.Queue.Count > 0);
        }

        [Fact]
        public void TestStopStartConditionalDeclarations()
        {
            var rabbitTemplate = new RabbitTemplate(connectionFactory);
            listenerContainer2.Start();
            listenerContainer2._startedLatch.Wait(TimeSpan.FromSeconds(10));

            rabbitTemplate.ConvertAndSend(Exch2, Q3, "foo");
            listener.Latch.Wait(TimeSpan.FromSeconds(10));
            Assert.True(listener.Queue.Count > 0);
            listenerContainer2.Stop();
            listenerContainer2.Start();
            listenerContainer1._startedLatch.Wait(TimeSpan.FromSeconds(10));
            rabbitTemplate.ConvertAndSend(Exch2, Q3, "foo");
            listener.Latch.Wait(TimeSpan.FromSeconds(10));
            Assert.True(listener.Queue.Count > 0);
        }

        [Fact]
        public void TestRedeclareXExpiresQueue()
        {
            var rabbitTemplate = new RabbitTemplate(connectionFactory);
            listenerContainer3.Start();
            listenerContainer3._startedLatch.Wait(TimeSpan.FromSeconds(10));
            rabbitTemplate.ConvertAndSend(expiringQueue.QueueName, "foo");
            listener.Latch.Wait(TimeSpan.FromSeconds(10));
            Assert.True(listener.Queue.Count > 0);

            listenerContainer3.Stop();
            listenerContainer3.Start();
            listenerContainer3._startedLatch.Wait(TimeSpan.FromSeconds(10));

            rabbitTemplate.ConvertAndSend(expiringQueue.QueueName, "foo");
            listener.Latch.Wait(TimeSpan.FromSeconds(10));
            Assert.True(listener.Queue.Count > 0);
        }

        [Fact]
        public void TestAutoDeclareFalse()
        {
            var rabbitTemplate = new RabbitTemplate(connectionFactory);
            rabbitTemplate.ConvertAndSend(Exch1, Q2, "foo");
            listener.Latch.Wait(TimeSpan.FromSeconds(10));
            Assert.True(listener.Queue.Count > 0);

            listenerContainer4.Stop();
            var testadminMock = new Mock<IRabbitAdmin>();
            testadminMock.Setup(m => m.Initialize()).Throws(new Exception("Shouldnt be called!"));
            listenerContainer4.RabbitAdmin = testadminMock.Object;
            listenerContainer4.Stop();
            listenerContainer4.Start();
            testadminMock.Verify(m => m.Initialize(), Times.Never);
        }

        private class AppendingListener : IReplyingMessageListener<string, string>
        {
            public AcknowledgeMode ContainerAckMode { get; set; }

            public ConcurrentQueue<string> Queue = new ConcurrentQueue<string>();
            public CountdownEvent Latch = new CountdownEvent(1);

            public string HandleMessage(string input)
            {
                Queue.Enqueue(input);
                Latch.Signal();
                return input;
            }

            public void Reset()
            {
                Queue.Clear();
            }
        }

        private class TestAdmin : RabbitAdmin
        {
            public int InstanceCounter = 0;

            public TestAdmin(Connection.IConnectionFactory connectionFactory, ILogger logger = null)
                : base(connectionFactory, logger)
            {

            }

            public new void Initialize()
            {
                Interlocked.Increment(ref InstanceCounter);
                base.Initialize();
            }
        }

    }
}
