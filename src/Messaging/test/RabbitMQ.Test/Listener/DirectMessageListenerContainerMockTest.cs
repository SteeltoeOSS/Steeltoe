// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Moq;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Listener
{
    public class DirectMessageListenerContainerMockTest
    {
        [Fact(Skip = "Not needed, We disable autorecoverconsumers")]
        public async Task TestAlwaysCancelAutoRecoverConsumer()
        {
            var connectionFactory = new Mock<Connection.IConnectionFactory>();
            var connection = new Mock<Connection.IConnection>();
            var channel = new Mock<IChannelProxy>();
            var rabbitChannel = new Mock<RC.IModel>();
            channel.Setup(c => c.TargetChannel).Returns(rabbitChannel.Object);

            connectionFactory.Setup((f) => f.CreateConnection()).Returns(connection.Object);
            connection.Setup((c) => c.CreateChannel(It.IsAny<bool>())).Returns(channel.Object);

            connection.Setup((c) => c.IsOpen).Returns(true);
            var isOpen = new AtomicBoolean(true);
            channel.Setup((c) => c.IsOpen).Returns(() => isOpen.Value);
            rabbitChannel.Setup(c => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());

            channel.Setup(c => c.QueueDeclarePassive(It.IsAny<string>())).Returns(new RC.QueueDeclareOk("test", 0, 0));
            channel.Setup(c =>
                c.BasicConsume(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<RC.IBasicConsumer>()))
                .Returns("consumerTag");

            var latch1 = new CountdownEvent(1);
            var qos = new AtomicInteger();
            channel.Setup(c => c.BasicQos(It.IsAny<uint>(), It.IsAny<ushort>(), It.IsAny<bool>()))
                .Callback<uint, ushort, bool>((size, count, global) =>
                {
                    qos.Value = count;
                    latch1.Signal();
                });

            var latch2 = new CountdownEvent(1);
            channel.Setup(c => c.BasicCancel("consumerTag"))
                .Callback(() => latch2.Signal());
            var container = new DirectMessageListenerContainer(null, connectionFactory.Object);
            container.SetQueueNames("test");
            container.PrefetchCount = 2;
            container.MonitorInterval = 100;
            container.Initialize();
            await container.Start();
            Assert.True(container._startedLatch.Wait(TimeSpan.FromSeconds(10)));

            Assert.True(latch1.Wait(TimeSpan.FromSeconds(10)));
            Assert.Equal(2, qos.Value);

            isOpen.Value = false;

            Assert.True(latch2.Wait(TimeSpan.FromSeconds(10)));
            await container.Stop();
        }

        [Fact]
        [Trait("Category", "SkipOnMacOS")] // TODO: Figure out why
        public async Task TestDeferredAcks()
        {
            var connectionFactory = new Mock<Connection.IConnectionFactory>();
            var connection = new Mock<Connection.IConnection>();
            var channel = new Mock<IChannelProxy>();
            var rabbitChannel = new Mock<RC.IModel>();
            channel.Setup(c => c.TargetChannel).Returns(rabbitChannel.Object);
            connectionFactory.Setup((f) => f.CreateConnection()).Returns(connection.Object);
            connection.Setup((c) => c.CreateChannel(It.IsAny<bool>())).Returns(channel.Object);
            connection.Setup((c) => c.IsOpen).Returns(true);
            channel.Setup((c) => c.IsOpen).Returns(true);
            rabbitChannel.Setup(c => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());
            channel.Setup(c => c.QueueDeclarePassive(It.IsAny<string>())).Returns(new RC.QueueDeclareOk("test", 0, 0));

            var consumer = new AtomicReference<RC.IBasicConsumer>();
            var latch1 = new CountdownEvent(1);
            channel.Setup(c =>
                c.BasicConsume(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<RC.IBasicConsumer>()))
                .Callback<string, bool, string, bool, bool, IDictionary<string, object>, RC.IBasicConsumer>(
                (queue, autoAck, consumerTag, noLocal, exclusive, args, cons) =>
                {
                    consumer.Value = cons;
                    cons.HandleBasicConsumeOk("consumerTag");
                    latch1.Signal();
                })
                .Returns("consumerTag");
            var qos = new AtomicInteger();
            channel.Setup(c => c.BasicQos(It.IsAny<uint>(), It.IsAny<ushort>(), It.IsAny<bool>()))
                .Callback<uint, ushort, bool>((size, count, global) =>
                {
                    qos.Value = count;
                });
            var latch2 = new CountdownEvent(1);
            var latch3 = new CountdownEvent(1);
            channel.Setup(c => c.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()))
                .Callback<ulong, bool>((tag, multi) =>
                {
                    if (tag == 10ul || tag == 16ul)
                    {
                        latch2.Signal();
                    }
                    else if (tag == 17ul)
                    {
                        latch3.Signal();
                    }
                });
            var latch4 = new CountdownEvent(1);
            channel.Setup(c => c.BasicNack(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Callback<ulong, bool, bool>((tag, multi, re) =>
                {
                    latch4.Signal();
                });
            var container = new DirectMessageListenerContainer(null, connectionFactory.Object);
            container.SetQueueNames("test");
            container.PrefetchCount = 2;
            container.MonitorInterval = 100;
            container.MessagesPerAck = 10;
            container.AckTimeout = 100;
            container.MessageListener = new TestListener();
            container.Initialize();
            await container.Start();
            Assert.True(container._startedLatch.Wait(TimeSpan.FromSeconds(10)));

            Assert.True(latch1.Wait(TimeSpan.FromSeconds(10)));
            Assert.Equal(10, qos.Value);
            var props = new MockRabbitBasicProperties();

            var body = new byte[1];
            for (long i = 1; i < 16; i++)
            {
                consumer.Value.HandleBasicDeliver("consumerTag", (ulong)i, false, string.Empty, string.Empty, props, body);
            }

            Thread.Sleep(200);

            consumer.Value.HandleBasicDeliver("consumerTag", 16ul, false, string.Empty, string.Empty, props, body);

            // should get 2 acks #10 and #16 (timeout)
            Assert.True(latch2.Wait(TimeSpan.FromSeconds(10)));
            consumer.Value.HandleBasicDeliver("consumerTag", 17ul, false, string.Empty, string.Empty, props, body);
            channel.Verify(c => c.BasicAck(10ul, true));
            channel.Verify(c => c.BasicAck(15ul, true));

            Assert.True(latch3.Wait(TimeSpan.FromSeconds(10)));

            // monitor task timeout
            channel.Verify(c => c.BasicAck(17ul, true));
            consumer.Value.HandleBasicDeliver("consumerTag", 18ul, false, string.Empty, string.Empty, props, body);
            consumer.Value.HandleBasicDeliver("consumerTag", 19ul, false, string.Empty, string.Empty, props, body);
            Assert.True(latch4.Wait(TimeSpan.FromSeconds(10)));

            // pending acks before nack
            channel.Verify(c => c.BasicAck(18ul, true));
            channel.Verify(c => c.BasicNack(19ul, true, true));
            consumer.Value.HandleBasicDeliver("consumerTag", 20ul, false, string.Empty, string.Empty, props, body);
            var latch5 = new CountdownEvent(1);
            channel.Setup(c => c.BasicCancel("consumerTag"))
                .Callback(() =>
                {
                    consumer.Value.HandleBasicCancelOk("consumerTag");
                    latch5.Signal();
                });

            await container.Stop();
            Assert.True(latch5.Wait(TimeSpan.FromSeconds(10)));
            channel.Verify((c) => c.BasicAck(20ul, true));
        }

        [Fact(Skip = "Fails, needs investigation")]
        public async Task TestRemoveQueuesWhileNotConnected()
        {
            var connectionFactory = new Mock<Connection.IConnectionFactory>();
            var connection = new Mock<Connection.IConnection>();
            var channel = new Mock<IChannelProxy>();
            var rabbitChannel = new Mock<RC.IModel>();
            channel.Setup(c => c.TargetChannel).Returns(rabbitChannel.Object);

            connectionFactory.Setup((f) => f.CreateConnection()).Returns(connection.Object);
            connection.Setup((c) => c.CreateChannel(It.IsAny<bool>())).Returns(channel.Object);
            connection.Setup((c) => c.IsOpen).Returns(true);
            var isOpen = new AtomicBoolean(true);
            channel.Setup((c) => c.IsOpen).Returns(
                () =>
                isOpen.Value);
            rabbitChannel.Setup(c => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());

            var declare = new AtomicReference<string>();
            channel.Setup(c => c.QueueDeclarePassive(It.IsAny<string>()))
                .Callback<string>(
                (name) =>
                declare.Value = name)
                .Returns(() =>
                    new RC.QueueDeclareOk(declare.Value, 0, 0));

            var latch1 = new CountdownEvent(2);
            var latch3 = new CountdownEvent(3);
            channel.Setup(c =>
                c.BasicConsume(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<RC.IBasicConsumer>()))
                .Callback(
                () =>
                {
                    if (!latch3.IsSet)
                    {
                        latch3.Signal();
                    }
                })
                .Returns("consumerTag");
            var qos = new AtomicInteger();
            channel.Setup(c => c.BasicQos(It.IsAny<uint>(), It.IsAny<ushort>(), It.IsAny<bool>()))
                .Callback<uint, ushort, bool>((size, count, global) =>
                {
                    qos.Value = count;
                    if (!latch1.IsSet)
                    {
                        latch1.Signal();
                    }
                });

            var latch2 = new CountdownEvent(2);
            channel.Setup(
                c => c.BasicCancel("consumerTag"))
                .Callback(
                () =>
                {
                    if (!latch2.IsSet)
                    {
                        latch2.Signal();
                    }
                });

            var container = new DirectMessageListenerContainer(null, connectionFactory.Object);
            container.SetQueueNames("test1", "test2");
            container.PrefetchCount = 2;
            container.MonitorInterval = 100;
            container.FailedDeclarationRetryInterval = 100;
            container.RecoveryInterval = 100;
            container.ShutdownTimeout = 1;
            container.Initialize();
            await container.Start();
            Assert.True(container._startedLatch.Wait(TimeSpan.FromSeconds(10)));

            Assert.True(latch1.Wait(TimeSpan.FromSeconds(10)));
            Assert.Equal(2, qos.Value);
            isOpen.Value = false;
            container.RemoveQueueNames("test1");
            Assert.True(latch2.Wait(TimeSpan.FromSeconds(20))); // Basic Cancels from isOpen = false
            isOpen.Value = true;  // Consumers should restart, but only test2,
            Assert.True(latch3.Wait(TimeSpan.FromSeconds(10)));

            channel.Verify(
                c =>
                c.BasicConsume("test1", It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<RC.IBasicConsumer>()),
                Times.Once());
            channel.Verify(
                c =>
                c.BasicConsume("test2", It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<RC.IBasicConsumer>()),
                Times.Exactly(2));
            await container.Stop();
        }

        [Fact]
        public async Task TestMonitorCancelsAfterBadAckEvenIfChannelReportsOpen()
        {
            var connectionFactory = new Mock<Connection.IConnectionFactory>();
            var connection = new Mock<Connection.IConnection>();
            var channel = new Mock<IChannelProxy>();
            var rabbitChannel = new Mock<RC.IModel>();
            channel.Setup(c => c.TargetChannel).Returns(rabbitChannel.Object);

            connectionFactory.Setup((f) => f.CreateConnection()).Returns(connection.Object);
            connection.Setup((c) => c.CreateChannel(It.IsAny<bool>())).Returns(channel.Object);
            connection.Setup((c) => c.IsOpen).Returns(true);
            channel.Setup(c => c.IsOpen).Returns(true);
            rabbitChannel.Setup(c => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());

            channel.Setup(c => c.QueueDeclarePassive(It.IsAny<string>())).Returns(new RC.QueueDeclareOk("test", 0, 0));

            var consumer = new AtomicReference<RC.IBasicConsumer>();
            var latch1 = new CountdownEvent(1);
            var latch2 = new CountdownEvent(1);
            channel.Setup(c =>
                c.BasicConsume(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<RC.IBasicConsumer>()))
                .Callback<string, bool, string, bool, bool, IDictionary<string, object>, RC.IBasicConsumer>(
                    (queue, autoAck, consumerTag, noLocal, exclusive, args, cons) =>
                    {
                        consumer.Value = cons;
                        latch1.Signal();
                    })
                .Returns("consumerTag");
            channel.Setup(c => c.BasicAck(1ul, false)).Throws(new Exception("bad ack"));
            channel.Setup(c => c.BasicCancel("consumerTag"))
                .Callback(() =>
                {
                    consumer.Value.HandleBasicCancelOk("consumerTag");
                    latch2.Signal();
                });
            var container = new DirectMessageListenerContainer(null, connectionFactory.Object);
            container.SetQueueNames("test");
            container.PrefetchCount = 2;
            container.MonitorInterval = 100;
            container.MessageListener = new Mock<IMessageListener>().Object;
            container.Initialize();
            await container.Start();
            Assert.True(container._startedLatch.Wait(TimeSpan.FromSeconds(10)));

            Assert.True(latch1.Wait(TimeSpan.FromSeconds(10)));
            var props = new MockRabbitBasicProperties();
            consumer.Value.HandleBasicDeliver("consumerTag", 1ul, false, string.Empty, string.Empty, props, new byte[1]);
            Assert.True(latch2.Wait(TimeSpan.FromSeconds(10)));
            await container.Stop();
        }

        [Fact]
        public async Task TestMonitorCancelsAfterTargetChannelChanges()
        {
            var connectionFactory = new Mock<Connection.IConnectionFactory>();
            var connection = new Mock<Connection.IConnection>();
            var channel = new Mock<IChannelProxy>();
            var rabbitChannel1 = new Mock<RC.IModel>();
            var rabbitChannel2 = new Mock<RC.IModel>();
            var target = new AtomicReference<RC.IModel>(rabbitChannel1.Object);
            channel.Setup(c => c.TargetChannel).Returns(() => target.Value);

            connectionFactory.Setup((f) => f.CreateConnection()).Returns(connection.Object);
            connection.Setup((c) => c.CreateChannel(It.IsAny<bool>())).Returns(channel.Object);
            connection.Setup((c) => c.IsOpen).Returns(true);
            channel.Setup(c => c.IsOpen).Returns(true);

            rabbitChannel1.Setup(c => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());
            rabbitChannel2.Setup(c => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());
            channel.Setup(c => c.QueueDeclarePassive(It.IsAny<string>())).Returns(new RC.QueueDeclareOk("test", 0, 0));

            var consumer = new AtomicReference<RC.IBasicConsumer>();
            var latch1 = new CountdownEvent(1);
            var latch2 = new CountdownEvent(1);
            channel.Setup(c =>
                c.BasicConsume(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<RC.IBasicConsumer>()))
                .Callback<string, bool, string, bool, bool, IDictionary<string, object>, RC.IBasicConsumer>(
                    (queue, autoAck, consumerTag, noLocal, exclusive, args, cons) =>
                    {
                        consumer.Value = cons;
                        latch1.Signal();
                    })
                .Returns("consumerTag");

            channel.Setup(c => c.BasicCancel("consumerTag"))
                .Callback(() =>
                {
                    consumer.Value.HandleBasicCancelOk("consumerTag");
                    latch2.Signal();
                });
            var container = new DirectMessageListenerContainer(null, connectionFactory.Object);
            container.SetQueueNames("test");
            container.PrefetchCount = 2;
            container.MonitorInterval = 100;
            container.MessageListener = new TestListener2(target, rabbitChannel2.Object);
            container.AcknowledgeMode = AcknowledgeMode.MANUAL;
            container.Initialize();
            await container.Start();
            Assert.True(container._startedLatch.Wait(TimeSpan.FromSeconds(10)));

            Assert.True(latch1.Wait(TimeSpan.FromSeconds(10)));
            var props = new MockRabbitBasicProperties();
            consumer.Value.HandleBasicDeliver("consumerTag", 1ul, false, string.Empty, string.Empty, props, new byte[1]);
            Assert.True(latch2.Wait(TimeSpan.FromSeconds(10)));
            await container.Stop();
        }

        private class TestListener2 : IMessageListener
        {
            private readonly AtomicReference<RC.IModel> target;
            private readonly RC.IModel @object;

            public TestListener2(AtomicReference<RC.IModel> target, RC.IModel @object)
            {
                this.target = target;
                this.@object = @object;
            }

            public AcknowledgeMode ContainerAckMode { get; set; }

            public void OnMessage(IMessage message)
            {
                target.Value = @object;
            }

            public void OnMessageBatch(List<IMessage> messages)
            {
                throw new NotImplementedException();
            }
        }

        private class TestListener : IMessageListener
        {
            public AcknowledgeMode ContainerAckMode { get; set; }

            public void OnMessage(IMessage message)
            {
                if (message.Headers.DeliveryTag().Value == 19ul)
                {
                    throw new Exception("TestNackAndPendingAcks");
                }
            }

            public void OnMessageBatch(List<IMessage> messages)
            {
            }
        }
    }
}
