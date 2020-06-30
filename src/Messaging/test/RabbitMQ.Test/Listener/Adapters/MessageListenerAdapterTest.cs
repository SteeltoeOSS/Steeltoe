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

using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Retry;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Rabbit.Core;
using Steeltoe.Messaging.Rabbit.Extensions;
using Steeltoe.Messaging.Rabbit.Support;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Messaging.Rabbit.Listener.Adapters
{
    public class MessageListenerAdapterTest
    {
        private readonly SimpleService simpleService = new SimpleService();

        private MessageHeaders messageProperties;

        private MessageListenerAdapter adapter;

        public MessageListenerAdapterTest()
        {
            var headers = new Dictionary<string, object>()
            {
                { MessageHeaders.CONTENT_TYPE,  MimeTypeUtils.TEXT_PLAIN_VALUE }
            };
            messageProperties = new MessageHeaders(headers);
            adapter = new MessageListenerAdapter(null);
        }

        [Fact]
        public void TestExtendedListenerAdapter()
        {
            var extendedAdapter = new ExtendedListenerAdapter(null);
            var called = new AtomicBoolean(false);
            var channelMock = new Mock<IModel>();
            var delgate = new TestDelegate(called);
            extendedAdapter.Instance = delgate;
            extendedAdapter.ContainerAckMode = Core.AcknowledgeMode.MANUAL;
            var bytes = EncodingUtils.GetDefaultEncoding().GetBytes("foo");
            extendedAdapter.OnMessage(Message.Create<byte[]>(bytes, messageProperties), channelMock.Object);
            Assert.True(called.Value);
        }

        [Fact]
        public void TestDefaultListenerMethod()
        {
            var called = new AtomicBoolean(false);
            var dele = new TestDelegate1(called);
            adapter.Instance = dele;
            var bytes = EncodingUtils.GetDefaultEncoding().GetBytes("foo");
            adapter.OnMessage(Message.Create(bytes, messageProperties), null);
            Assert.True(called.Value);
        }

        [Fact]
        public void TestAlternateConstructor()
        {
            var called = new AtomicBoolean(false);
            var dele = new TestDelegate2(called);
            adapter = new MessageListenerAdapter(null, dele, "MyPojoMessageMethod");
            var bytes = EncodingUtils.GetDefaultEncoding().GetBytes("foo");
            adapter.OnMessage(Message.Create(bytes, messageProperties), null);
            Assert.True(called.Value);
        }

        [Fact]
        public void TestExplicitListenerMethod()
        {
            adapter.DefaultListenerMethod = "Handle";
            adapter.Instance = simpleService;
            var bytes = EncodingUtils.GetDefaultEncoding().GetBytes("foo");
            adapter.OnMessage(Message.Create(bytes, messageProperties), null);
            Assert.Equal("Handle", simpleService.Called);
        }

        [Fact]
        public void TestMappedListenerMethod()
        {
            var map = new Dictionary<string, string>();
            map.Add("foo", "Handle");
            map.Add("bar", "NotDefinedOnInterface");
            adapter.DefaultListenerMethod = "AnotherHandle";
            adapter.SetQueueOrTagToMethodName(map);
            adapter.Instance = simpleService;
            var bytes = EncodingUtils.GetDefaultEncoding().GetBytes("foo");
            var message = Message.Create(bytes, messageProperties);
            var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            accessor.ConsumerQueue = "foo";
            accessor.ConsumerTag = "bar";
            adapter.OnMessage(message, null);
            Assert.Equal("Handle", simpleService.Called);
            message = Message.Create(bytes, messageProperties);
            accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            accessor.ConsumerQueue = "junk";
            adapter.OnMessage(message, null);
            Assert.Equal("NotDefinedOnInterface", simpleService.Called);
            message = Message.Create(bytes, messageProperties);
            accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            accessor.ConsumerTag = "junk";
            adapter.OnMessage(message, null);
            Assert.Equal("AnotherHandle", simpleService.Called);
        }

        [Fact]
        public void TestReplyRetry()
        {
            adapter.DefaultListenerMethod = "Handle";
            adapter.Instance = simpleService;
            adapter.RetryTemplate = new PollyRetryTemplate(new Dictionary<Type, bool>(), 2, true, 1, 1, 1);
            var replyMessage = new AtomicReference<IMessage>();
            var replyAddress = new AtomicReference<Address>();
            var throwable = new AtomicReference<Exception>();
            adapter.RecoveryCallback = new TestRecoveryCallback(replyMessage, replyAddress, throwable);

            var accessor = RabbitHeaderAccessor.GetMutableAccessor(messageProperties);
            accessor.ReplyTo = "foo/bar";
            var ex = new Exception();
            var mockChannel = new Mock<IModel>();
            mockChannel.Setup(c => c.BasicPublish("foo", "bar", false, It.IsAny<IBasicProperties>(), It.IsAny<byte[]>()))
                .Throws(ex);
            mockChannel.Setup(c => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());
            var bytes = EncodingUtils.GetDefaultEncoding().GetBytes("foo");
            var message = Message.Create(bytes, messageProperties);
            adapter.OnMessage(message, mockChannel.Object);
            Assert.Equal("Handle", simpleService.Called);
            Assert.NotNull(replyMessage.Value);
            var reply = EncodingUtils.GetDefaultEncoding().GetString((byte[])replyMessage.Value.Payload);
            Assert.NotNull(replyAddress.Value);
            var addr = replyAddress.Value;
            Assert.Equal("foo", addr.ExchangeName);
            Assert.Equal("bar", addr.RoutingKey);
            Assert.Same(ex, throwable.Value);
        }

        [Fact]
        public void TestTaskReturn()
        {
            var called = new CountdownEvent(1);
            var dele = new TestAsyncDelegate();
            adapter = new MessageListenerAdapter(null, dele, "MyPojoMessageMethod");
            adapter.ContainerAckMode = AcknowledgeMode.MANUAL;
            adapter.ResponseExchange = "default";
            var mockChannel = new Mock<IModel>();
            mockChannel.Setup(c => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());
            mockChannel.Setup(c => c.BasicAck(It.IsAny<ulong>(), false))
                .Callback(() => called.Signal());
            var bytes = EncodingUtils.GetDefaultEncoding().GetBytes("foo");
            var message = Message.Create(bytes, messageProperties);
            adapter.OnMessage(message, mockChannel.Object);
            Assert.True(called.Wait(TimeSpan.FromSeconds(10)));
        }

        public class TestRecoveryCallback : IRecoveryCallback
        {
            private AtomicReference<IMessage> replyMessage;
            private AtomicReference<Address> replyAddress;
            private AtomicReference<Exception> throwable;

            public TestRecoveryCallback(AtomicReference<IMessage> replyMessage, AtomicReference<Address> replyAddress, AtomicReference<Exception> throwable)
            {
                this.replyMessage = replyMessage;
                this.replyAddress = replyAddress;
                this.throwable = throwable;
            }

            public object Recover(IRetryContext context)
            {
                replyMessage.Value = SendRetryContextAccessor.GetMessage(context);
                replyAddress.Value = SendRetryContextAccessor.GetAddress(context);
                throwable.Value = context.LastException;
                return null;
            }
        }

        public interface IService
        {
            string Handle(string input);

            string AnotherHandle(string input);
        }

        public class SimpleService : IService
        {
            public string Called;

            public string Handle(string input)
            {
                Called = "Handle";
                return "processed" + input;
            }

            public string AnotherHandle(string input)
            {
                Called = "AnotherHandle";
                return "processed" + input;
            }

            public string NotDefinedOnInterface(string input)
            {
                Called = "NotDefinedOnInterface";
                return "processed" + input;
            }
        }

        private class TestAsyncDelegate
        {
            public TestAsyncDelegate()
            {
            }

            public Task<string> MyPojoMessageMethod(string input)
            {
                return Task.Run(() =>
                {
                    Thread.Sleep(1000);
                    return "processed" + input;
                });
            }
        }

        private class TestDelegate2
        {
            private AtomicBoolean called;

            public TestDelegate2(AtomicBoolean called)
            {
                this.called = called;
            }

            public string MyPojoMessageMethod(string input)
            {
                called.Value = true;
                return "processed" + input;
            }
        }

        private class TestDelegate1
        {
            private AtomicBoolean called;

            public TestDelegate1(AtomicBoolean called)
            {
                this.called = called;
            }

            public string HandleMessage(string input)
            {
                called.Value = true;
                return "processed" + input;
            }
        }

        private class TestDelegate
        {
            private AtomicBoolean called;

            public TestDelegate(AtomicBoolean called)
            {
                this.called = called;
            }

            public void HandleMessage(string input, IModel channel, IMessage message)
            {
                Assert.NotNull(input);
                Assert.NotNull(channel);
                Assert.NotNull(message);
                ulong deliveryTag = 0;
                if (message.Headers.DeliveryTag().HasValue)
                {
                    deliveryTag = message.Headers.DeliveryTag().Value;
                }

                channel.BasicAck(deliveryTag, false);
                called.Value = true;
            }
        }

        private class ExtendedListenerAdapter : MessageListenerAdapter
        {
            public ExtendedListenerAdapter(IApplicationContext context, ILogger logger = null)
                : base(context, logger)
            {
            }

            protected override object[] BuildListenerArguments(object extractedMessage, IModel channel, IMessage message)
            {
                return new object[] { extractedMessage, channel, message };
            }
        }
    }
}
