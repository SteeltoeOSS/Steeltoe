﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Moq;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Attributes.Support;
using Steeltoe.Messaging.Handler.Invocation;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Listener.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Support;
using Steeltoe.Messaging.RabbitMQ.Test;
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Listener.Adapters
{
    public class MessagingMessageListenerAdapterTest : AbstractTest
    {
        private readonly DefaultMessageHandlerMethodFactory factory = new DefaultMessageHandlerMethodFactory();
        private readonly SampleBean sample = new SampleBean();

        [Fact]
        public void BuildMessageWithStandardMessage()
        {
            var result = RabbitMessageBuilder.WithPayload("Response")
                .SetHeader("foo", "bar")
                .SetHeader(RabbitMessageHeaders.TYPE, "msg_type")
                .SetHeader(RabbitMessageHeaders.REPLY_TO, "reply")
                .Build();
            var session = new Mock<RC.IModel>();
            var listener = GetSimpleInstance("Echo", typeof(IMessage<string>));
            var replyMessage = listener.BuildMessage(session.Object, result, null);
            Assert.NotNull(replyMessage);
            Assert.Equal("Response", EncodingUtils.GetDefaultEncoding().GetString(replyMessage.Payload));
            Assert.Equal("msg_type", replyMessage.Headers.Type());
            Assert.Equal("reply", replyMessage.Headers.ReplyTo());
            Assert.Equal("bar", replyMessage.Headers.Get<string>("foo"));
        }

        [Fact]
        public void ExceptionInListener()
        {
            var message = MessageTestUtils.CreateTextMessage("foo");
            var mockChannel = new Mock<RC.IModel>();
            mockChannel.Setup(c => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());
            var listener = GetSimpleInstance("Fail", typeof(string));
            try
            {
                listener.OnMessage(message, mockChannel.Object);
                throw new Exception("Should have thrown an exception");
            }
            catch (ListenerExecutionFailedException ex)
            {
                Assert.IsType<ArgumentException>(ex.InnerException);
                Assert.Contains("Expected test exception", ex.InnerException.Message);
            }
            catch (Exception ex)
            {
                throw new Exception("Should not have thrown an " + ex);
            }
        }

        [Fact]
        public void ExceptionInListenerBadReturnExceptionSetting()
        {
            var message = MessageTestUtils.CreateTextMessage("foo");
            var mockChannel = new Mock<RC.IModel>();
            mockChannel.Setup(c => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());
            var listener = GetSimpleInstance("Fail", true, typeof(string));
            try
            {
                listener.OnMessage(message, mockChannel.Object);
                throw new Exception("Should have thrown an exception");
            }
            catch (ListenerExecutionFailedException ex)
            {
                Assert.IsType<ArgumentException>(ex.InnerException);
                Assert.Contains("Expected test exception", ex.InnerException.Message);
            }
            catch (Exception ex)
            {
                throw new Exception("Should not have thrown an " + ex);
            }
        }

        [Fact]
        public void ExceptionInMultiListenerReturnException()
        {
            var message = MessageTestUtils.CreateTextMessage("foo");
            var mockChannel = new Mock<RC.IModel>();
            mockChannel.Setup(c => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());
            var listener = GetMultiInstance("Fail", "FailWithReturn", true, typeof(string), typeof(byte[]));
            try
            {
                listener.OnMessage(message, mockChannel.Object);
                throw new Exception("Should have thrown an exception");
            }
            catch (ListenerExecutionFailedException ex)
            {
                Assert.IsType<ArgumentException>(ex.InnerException);
                Assert.Contains("Expected test exception", ex.InnerException.Message);
            }
            catch (Exception ex)
            {
                throw new Exception("Should not have thrown an " + ex);
            }

            message = Message.Create(new byte[] { 1, 2 }, new MessageHeaders());
            try
            {
                listener.OnMessage(message, mockChannel.Object);
                throw new Exception("Should have thrown an exception");
            }
            catch (ReplyFailureException ex)
            {
                Assert.Contains("Failed to send reply", ex.Message);
            }
            catch (Exception ex)
            {
                throw new Exception("Should not have thrown an " + ex);
            }

            // TODO: The Java simpleconverter will convert the exception using java serialization...
            var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            accessor.ReplyTo = "foo/bar";
            listener.OnMessage(message, mockChannel.Object);

            // TODO: verify(channel).basicPublish(eq("foo"), eq("bar"), eq(false), any(BasicProperties.class), any(byte[].class));
        }

        [Fact]
        public void ExceptionInInvocation()
        {
            var message = MessageTestUtils.CreateTextMessage("foo");
            var mockChannel = new Mock<RC.IModel>();
            mockChannel.Setup(c => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());
            var listener = GetSimpleInstance("WrongParam", typeof(int));
            try
            {
                listener.OnMessage(message, mockChannel.Object);
                throw new Exception("Should have thrown an exception");
            }
            catch (ListenerExecutionFailedException ex)
            {
                Assert.IsType<MessageConversionException>(ex.InnerException);
            }
            catch (Exception ex)
            {
                throw new Exception("Should not have thrown an " + ex);
            }
        }

        [Fact]
        public void GenericMessageTest1()
        {
            var message = MessageTestUtils.CreateTextMessage("\"foo\"");
            var mockChannel = new Mock<RC.IModel>();
            mockChannel.Setup(c => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());
            var listener = GetSimpleInstance("WithGenericMessageObjectType", typeof(IMessage<object>));
            listener.MessageConverter = new RabbitMQ.Support.Converter.JsonMessageConverter();
            var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            accessor.ContentType = MimeTypeUtils.APPLICATION_JSON_VALUE;
            listener.OnMessage(message, mockChannel.Object);
            Assert.IsType<string>(sample.Payload);
        }

        [Fact]
        public void GenericMessageTest2()
        {
            var message = MessageTestUtils.CreateTextMessage("{ \"foostring\" : \"bar\" }");
            var mockChannel = new Mock<RC.IModel>();
            mockChannel.Setup(c => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());
            var listener = GetSimpleInstance("WithGenericMessageFooType", typeof(IMessage<Foo>));
            listener.MessageConverter = new RabbitMQ.Support.Converter.JsonMessageConverter();
            var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            accessor.ContentType = MimeTypeUtils.APPLICATION_JSON_VALUE;
            listener.OnMessage(message, mockChannel.Object);
            Assert.IsType<Foo>(sample.Payload);
        }

        [Fact]
        public void GenericMessageTest3()
        {
            var message = MessageTestUtils.CreateTextMessage("\"foo\"");
            var mockChannel = new Mock<RC.IModel>();
            mockChannel.Setup(c => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());
            var listener = GetSimpleInstance("WithNonGenericMessage", typeof(IMessage));
            listener.MessageConverter = new RabbitMQ.Support.Converter.JsonMessageConverter();
            var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            accessor.ContentType = MimeTypeUtils.APPLICATION_JSON_VALUE;
            listener.OnMessage(message, mockChannel.Object);
            Assert.IsType<string>(sample.Payload);
        }

        [Fact]
        public void GenericMessageTest4()
        {
            var message = MessageTestUtils.CreateTextMessage("{ \"foo\" : \"bar\" }");
            var mockChannel = new Mock<RC.IModel>();
            mockChannel.Setup(c => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());
            var listener = GetSimpleInstance("WithGenericMessageDictionaryType", typeof(IMessage<Dictionary<string, string>>));
            listener.MessageConverter = new RabbitMQ.Support.Converter.JsonMessageConverter();
            var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            accessor.ContentType = MimeTypeUtils.APPLICATION_JSON_VALUE;
            listener.OnMessage(message, mockChannel.Object);
            Assert.IsType<Dictionary<string, string>>(sample.Payload);
        }

        [Fact]
        public void BatchMessagesTest()
        {
            var message = MessageTestUtils.CreateTextMessage("{ \"foo1\" : \"bar1\" }");
            var mockChannel = new Mock<RC.IModel>();
            mockChannel.Setup(c => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());
            var listener = GetBatchInstance("WithMessageBatch", typeof(List<IMessage<byte[]>>));
            listener.MessageConverter = new RabbitMQ.Support.Converter.JsonMessageConverter();
            var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            accessor.ContentType = MimeTypeUtils.APPLICATION_JSON_VALUE;
            listener.OnMessageBatch(new List<IMessage>() { message }, mockChannel.Object);
            Assert.IsType<string>(sample.BatchPayloads[0]);
        }

        [Fact]
        public void BatchTypedMessagesTest()
        {
            var message = MessageTestUtils.CreateTextMessage("{ \"foostring\" : \"bar1\" }");
            var mockChannel = new Mock<RC.IModel>();
            mockChannel.Setup(c => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());
            var listener = GetBatchInstance("WithTypedMessageBatch", typeof(List<IMessage<Foo>>));
            listener.MessageConverter = new RabbitMQ.Support.Converter.JsonMessageConverter();
            var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            accessor.ContentType = MimeTypeUtils.APPLICATION_JSON_VALUE;
            listener.OnMessageBatch(new List<IMessage>() { message }, mockChannel.Object);
            Assert.IsType<Foo>(sample.BatchPayloads[0]);
        }

        [Fact]
        public void BatchTypedObjectTest()
        {
            var message = MessageTestUtils.CreateTextMessage("{ \"foostring\" : \"bar1\" }");
            var mockChannel = new Mock<RC.IModel>();
            mockChannel.Setup(c => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());
            var listener = GetBatchInstance("WithFooBatch", typeof(List<Foo>));
            listener.MessageConverter = new RabbitMQ.Support.Converter.JsonMessageConverter();
            var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            accessor.ContentType = MimeTypeUtils.APPLICATION_JSON_VALUE;
            listener.OnMessageBatch(new List<IMessage>() { message }, mockChannel.Object);
            Assert.IsType<Foo>(sample.BatchPayloads[0]);
        }

        protected MessagingMessageListenerAdapter GetSimpleInstance(string methodName, params Type[] parameterTypes)
        {
            var m = typeof(SampleBean).GetMethod(methodName, parameterTypes);
            return CreateInstance(m, false);
        }

        protected MessagingMessageListenerAdapter GetSimpleInstance(string methodName, bool returnExceptions, params Type[] parameterTypes)
        {
            var m = typeof(SampleBean).GetMethod(methodName, parameterTypes);
            return CreateInstance(m, returnExceptions);
        }

        protected MessagingMessageListenerAdapter GetMultiInstance(string methodName1, string methodName2, bool returnExceptions, Type m1ParameterType, Type m2ParameterType)
        {
            var m1 = typeof(SampleBean).GetMethod(methodName1, new Type[] { m1ParameterType });
            var m2 = typeof(SampleBean).GetMethod(methodName2, new Type[] { m2ParameterType });
            return CreateMultiInstance(m1, m2, returnExceptions);
        }

        protected MessagingMessageListenerAdapter CreateMultiInstance(MethodInfo m1, MethodInfo m2, bool returnExceptions)
        {
            var adapter = new MessagingMessageListenerAdapter(null, null, null, returnExceptions, null);
            var methods = new List<IInvocableHandlerMethod>
            {
                factory.CreateInvocableHandlerMethod(sample, m1),
                factory.CreateInvocableHandlerMethod(sample, m2)
            };
            var handler = new DelegatingInvocableHandler(methods, sample, null, null);
            adapter.HandlerAdapter = new HandlerAdapter(handler);
            return adapter;
        }

        protected MessagingMessageListenerAdapter CreateInstance(MethodInfo m, bool returnExceptions)
        {
            var adapter = new MessagingMessageListenerAdapter(null, null, m, returnExceptions, null)
            {
                HandlerAdapter = new HandlerAdapter(factory.CreateInvocableHandlerMethod(sample, m))
            };
            return adapter;
        }

        protected BatchMessagingMessageListenerAdapter GetBatchInstance(string methodName, params Type[] parameterTypes)
        {
            var m = typeof(SampleBean).GetMethod(methodName, parameterTypes);
            return CreateBatchInstance(m);
        }

        protected BatchMessagingMessageListenerAdapter CreateBatchInstance(MethodInfo m)
        {
            var adapter = new BatchMessagingMessageListenerAdapter(null, null, m, false, null, null)
            {
                HandlerAdapter = new HandlerAdapter(factory.CreateInvocableHandlerMethod(sample, m))
            };
            return adapter;
        }

        private class SampleBean
        {
            public object Payload;
            public List<object> BatchPayloads;

            public SampleBean()
            {
            }

            public IMessage<string> Echo(IMessage<string> input)
            {
                return (IMessage<string>)RabbitMessageBuilder.WithPayload(input.Payload)
                        .SetHeader(RabbitMessageHeaders.TYPE, "reply")
                        .Build();
            }

            public void Fail(string input)
            {
                throw new ArgumentException("Expected test exception");
            }

            public void WrongParam(int i)
            {
                throw new ArgumentException("Should not have been called");
            }

            public void WithGenericMessageObjectType(IMessage<object> message)
            {
                Payload = message.Payload;
            }

            public void WithFoo(Foo foo)
            {
                Payload = foo;
            }

            public void WithGenericMessageFooType(IMessage<Foo> message)
            {
                Payload = message.Payload;
            }

            public void WithGenericMessageDictionaryType(IMessage<Dictionary<string, string>> message)
            {
                Payload = message.Payload;
            }

            public void WithNonGenericMessage(IMessage message)
            {
                Payload = message.Payload;
            }

            public void WithMessageBatch(List<IMessage<byte[]>> messageBatch)
            {
                BatchPayloads = new List<object>();
                foreach (var m in messageBatch)
                {
                    BatchPayloads.Add(EncodingUtils.GetDefaultEncoding().GetString((byte[])m.Payload));
                }
            }

            public void WithTypedMessageBatch(List<IMessage<Foo>> messageBatch)
            {
                BatchPayloads = new List<object>();
                foreach (var m in messageBatch)
                {
                    BatchPayloads.Add(m.Payload);
                }
            }

            public void WithFooBatch(List<Foo> messageBatch)
            {
                BatchPayloads = new List<object>();
                foreach (var m in messageBatch)
                {
                    BatchPayloads.Add(m);
                }
            }

            public string FailWithReturn(byte[] input)
            {
                throw new ArgumentException("Expected test exception");
            }
        }

        private class Foo
        {
            public string FooString { get; set; }
        }
    }
}
