// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Acks;
using Steeltoe.Integration.Rabbit.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.RabbitMQ;
using Steeltoe.Messaging.RabbitMQ.Batch;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Support;
using System.Collections.Generic;
using System.Text;
using Xunit;
using R = RabbitMQ.Client;

namespace Steeltoe.Integration.Rabbit.Inbound
{
    public class RabbitMessageSourceTest
    {
        [Fact]
        public void TestAck()
        {
            var config = new ConfigurationBuilder().Build();
            var services = new ServiceCollection().BuildServiceProvider();
            var context = new GenericApplicationContext(services, config);
            var channel = new Mock<R.IModel>();
            channel.Setup(c => c.IsOpen).Returns(true);
            var props = new MockRabbitBasicProperties();
            var getResponse = new R.BasicGetResult(123Ul, false, "ex", "rk", 0, props, Encoding.UTF8.GetBytes("foo"));
            channel.Setup(c => c.BasicGet("foo", false)).Returns(getResponse);
            var connection = new Mock<R.IConnection>();
            connection.Setup(c => c.IsOpen).Returns(true);
            connection.Setup(c => c.CreateModel()).Returns(channel.Object);
            var connectionFactory = new Mock<R.IConnectionFactory>();
            connectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>())).Returns(connection.Object);

            CachingConnectionFactory ccf = new CachingConnectionFactory(connectionFactory.Object);
            var source = new RabbitMessageSource(context, ccf, "foo");
            source.RawMessageHeader = true;
            var received = source.Receive();
            var rawMessage = received.Headers.Get<IMessage>(RabbitMessageHeaderErrorMessageStrategy.AMQP_RAW_MESSAGE);
            var sourceData = received.Headers.Get<IMessage>(IntegrationMessageHeaderAccessor.SOURCE_DATA);
            Assert.NotNull(rawMessage);
            Assert.Same(rawMessage, sourceData);
            Assert.Equal("foo", received.Headers.Get<string>(RabbitMessageHeaders.CONSUMER_QUEUE));

            // make sure channel is not cached
            var conn = ccf.CreateConnection();
            var notCached = conn.CreateChannel(false);
            connection.Verify(c => c.CreateModel(), Times.Exactly(2));
            var callback = received.Headers.Get<IAcknowledgmentCallback>(IntegrationMessageHeaderAccessor.ACKNOWLEDGMENT_CALLBACK);
            callback.Acknowledge(Status.ACCEPT);
            channel.Verify(c => c.BasicAck(123ul, false));
            var cached = conn.CreateChannel(false); // should have been "closed"
            connection.Verify(c => c.CreateModel(), Times.Exactly(2));
            notCached.Close();
            cached.Close();
            ccf.Destroy();
            channel.Verify(c => c.Close(), Times.Exactly(2));
            connection.Verify(c => c.Close(30000));
        }

        [Fact]
        public void TestNAck()
        {
            TestNackOrRequeue(false);
        }

        [Fact]
        public void TestRequeue()
        {
            TestNackOrRequeue(false);
        }

        [Fact]
        public void TestBatch()
        {
            var bs = new SimpleBatchingStrategy(2, 10_000, 10_000L);
            var headers = new RabbitHeaderAccessor();
            headers.ContentType = "test/plain";
            var message = Message.Create(Encoding.UTF8.GetBytes("test1"), headers.MessageHeaders);
            bs.AddToBatch("foo", "bar", message);
            message = Message.Create(Encoding.UTF8.GetBytes("test2"), headers.MessageHeaders);
            var batched = bs.AddToBatch("foo", "bar", message);
            Assert.True(batched.HasValue);
            var batchMessage = batched.Value.Message;
            var batchHeaders = batchMessage.Headers;
            var headerConverter = new DefaultMessageHeadersConverter();
            var props = new MockRabbitBasicProperties();
            headerConverter.FromMessageHeaders(batchHeaders, props, Encoding.UTF8);
            props.ContentType = "text/plain";

            var config = new ConfigurationBuilder().Build();
            var services = new ServiceCollection().BuildServiceProvider();
            var context = new GenericApplicationContext(services, config);
            var channel = new Mock<R.IModel>();
            channel.Setup(c => c.IsOpen).Returns(true);

            var getResponse = new R.BasicGetResult(123Ul, false, "ex", "rk", 0, props, (byte[])batchMessage.Payload);
            channel.Setup(c => c.BasicGet("foo", false)).Returns(getResponse);
            var connection = new Mock<R.IConnection>();
            connection.Setup(c => c.IsOpen).Returns(true);
            connection.Setup(c => c.CreateModel()).Returns(channel.Object);
            var connectionFactory = new Mock<R.IConnectionFactory>();
            connectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>())).Returns(connection.Object);

            CachingConnectionFactory ccf = new CachingConnectionFactory(connectionFactory.Object);
            var source = new RabbitMessageSource(context, ccf, "foo");
            var received = source.Receive();
            Assert.NotNull(received);
            var asList = received.Payload as List<object>;
            Assert.NotNull(asList);
            Assert.Contains("test1", asList);
            Assert.Contains("test2", asList);
        }

        private void TestNackOrRequeue(bool requeue)
        {
            var config = new ConfigurationBuilder().Build();
            var services = new ServiceCollection().BuildServiceProvider();
            var context = new GenericApplicationContext(services, config);

            var channel = new Mock<R.IModel>();
            channel.Setup(c => c.IsOpen).Returns(true);
            var props = new MockRabbitBasicProperties();
            var getResponse = new R.BasicGetResult(123Ul, false, "ex", "rk", 0, props, Encoding.UTF8.GetBytes("bar"));
            channel.Setup(c => c.BasicGet("foo", false)).Returns(getResponse);
            var connection = new Mock<R.IConnection>();
            connection.Setup(c => c.IsOpen).Returns(true);
            connection.Setup(c => c.CreateModel()).Returns(channel.Object);
            var connectionFactory = new Mock<R.IConnectionFactory>();
            connectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>())).Returns(connection.Object);

            CachingConnectionFactory ccf = new CachingConnectionFactory(connectionFactory.Object);
            var source = new RabbitMessageSource(context, ccf, "foo");
            var received = source.Receive();
            connection.Verify(c => c.CreateModel());
            var callback = received.Headers.Get<IAcknowledgmentCallback>(IntegrationMessageHeaderAccessor.ACKNOWLEDGMENT_CALLBACK);
            if (requeue)
            {
                callback.Acknowledge(Status.REQUEUE);
            }
            else
            {
                callback.Acknowledge(Status.REJECT);
            }

            channel.Verify(c => c.BasicReject(123ul, requeue));
            connection.Verify(c => c.CreateModel());
            ccf.Destroy();
            channel.Verify(c => c.Close());
            connection.Verify(c => c.Close(30000));
        }
    }
}
