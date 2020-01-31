﻿// Copyright 2017 the original author or authors.
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

using Moq;
using Steeltoe.Messaging.Support;
using Steeltoe.Messaging.Test;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Messaging.Core.Test
{
    public class GenericMessagingTemplateTest
    {
        internal GenericMessagingTemplate Template;

        internal StubMessageChannel MessageChannel;

        public GenericMessagingTemplateTest()
        {
            MessageChannel = new StubMessageChannel();
            Template = new GenericMessagingTemplate();
            Template.DefaultDestination = MessageChannel;
            Template.DestinationResolver = new TestDestinationResolver(this);
        }

        [Fact]
        public void SendWithTimeout()
        {
            IMessage sent = null;

            var chanMock = new Mock<ISubscribableChannel>();
            var channel = chanMock.Object;
            chanMock.Setup(
                chan => chan.Send(It.IsAny<IMessage>(), It.Is<int>(i => i == 30000)))
                .Callback<IMessage, int>((m, t) => sent = m)
                .Returns(true);

            var message = MessageBuilder.WithPayload("request")
                    .SetHeader(GenericMessagingTemplate.DEFAULT_SEND_TIMEOUT_HEADER, 30000)
                    .SetHeader(GenericMessagingTemplate.DEFAULT_RECEIVE_TIMEOUT_HEADER, 1)
                    .Build();

            Template.Send(channel, message);

            chanMock.Verify(chan => chan.Send(It.IsAny<IMessage>(), It.Is<int>(i => i == 30000)));
            Assert.NotNull(sent);
            Assert.False(sent.Headers.ContainsKey(GenericMessagingTemplate.DEFAULT_SEND_TIMEOUT_HEADER));
            Assert.False(sent.Headers.ContainsKey(GenericMessagingTemplate.DEFAULT_RECEIVE_TIMEOUT_HEADER));
        }

        [Fact]
        public async Task SendAsyncWithTimeout()
        {
            IMessage sent = null;

            var chanMock = new Mock<ISubscribableChannel>();
            var channel = chanMock.Object;
            chanMock.Setup(
                chan => chan.SendAsync(It.IsAny<IMessage>(), It.Is<CancellationToken>(t => t.IsCancellationRequested == false)))
                .Callback<IMessage, CancellationToken>((m, t) => sent = m)
                .Returns(new ValueTask<bool>(true));

            var message = MessageBuilder.WithPayload("request")
                    .SetHeader(GenericMessagingTemplate.DEFAULT_SEND_TIMEOUT_HEADER, 30000)
                    .SetHeader(GenericMessagingTemplate.DEFAULT_RECEIVE_TIMEOUT_HEADER, 1)
                    .Build();

            await Template.SendAsync(channel, message);

            chanMock.Verify(chan => chan.SendAsync(It.IsAny<IMessage>(), It.Is<CancellationToken>(t => t.IsCancellationRequested == false)));
            Assert.NotNull(sent);
            Assert.False(sent.Headers.ContainsKey(GenericMessagingTemplate.DEFAULT_SEND_TIMEOUT_HEADER));
            Assert.False(sent.Headers.ContainsKey(GenericMessagingTemplate.DEFAULT_RECEIVE_TIMEOUT_HEADER));
        }

        [Fact]
        public async Task SendAsyncWithTimeoutMutable()
        {
            IMessage sent = null;

            var chanMock = new Mock<ISubscribableChannel>();
            var channel = chanMock.Object;
            chanMock.Setup(
                chan => chan.SendAsync(It.IsAny<IMessage>(), It.Is<CancellationToken>(t => t.IsCancellationRequested == false)))
                .Callback<IMessage, CancellationToken>((m, t) => sent = m)
                .Returns(new ValueTask<bool>(true));

            var accessor = new MessageHeaderAccessor();
            accessor.LeaveMutable = true;
            var message = new GenericMessage<string>("request", accessor.MessageHeaders);
            accessor.SetHeader(GenericMessagingTemplate.DEFAULT_SEND_TIMEOUT_HEADER, 30000);
            await Template.SendAsync(channel, message);
            chanMock.Verify(chan => chan.SendAsync(It.IsAny<IMessage>(), It.Is<CancellationToken>(t => t.IsCancellationRequested == false)));
            Assert.NotNull(sent);
            Assert.False(sent.Headers.ContainsKey(GenericMessagingTemplate.DEFAULT_SEND_TIMEOUT_HEADER));
            Assert.False(sent.Headers.ContainsKey(GenericMessagingTemplate.DEFAULT_RECEIVE_TIMEOUT_HEADER));
        }

        [Fact]
        public void SendWithTimeoutMutable()
        {
            IMessage sent = null;

            var chanMock = new Mock<ISubscribableChannel>();
            var channel = chanMock.Object;
            chanMock.Setup(
                chan => chan.Send(It.IsAny<IMessage>(), It.Is<int>(i => i == 30000)))
                .Callback<IMessage, int>((m, t) => sent = m)
                .Returns(true);

            var accessor = new MessageHeaderAccessor();
            accessor.LeaveMutable = true;
            var message = new GenericMessage<string>("request", accessor.MessageHeaders);
            accessor.SetHeader(GenericMessagingTemplate.DEFAULT_SEND_TIMEOUT_HEADER, 30000);
            Template.Send(channel, message);
            chanMock.Verify(chan => chan.Send(It.IsAny<IMessage>(), It.Is<int>(i => i == 30000)));
            Assert.NotNull(sent);
            Assert.False(sent.Headers.ContainsKey(GenericMessagingTemplate.DEFAULT_SEND_TIMEOUT_HEADER));
            Assert.False(sent.Headers.ContainsKey(GenericMessagingTemplate.DEFAULT_RECEIVE_TIMEOUT_HEADER));
        }

        [Fact]
        public void SendAndReceive()
        {
            var channel = new TaskSchedulerSubscribableChannel(TaskScheduler.Default);
            channel.Subscribe(new SendAndReceiveTestHandler());

            var actual = Template.ConvertSendAndReceive<string>(channel, "request");
            Assert.Equal("response", actual);
        }

        [Fact]
        public async Task SendAndReceiveAsync()
        {
            var channel = new TaskSchedulerSubscribableChannel(TaskScheduler.Default);
            channel.Subscribe(new SendAndReceiveTestHandler());

            var actual = await Template.ConvertSendAndReceiveAsync<string>(channel, "request");
            Assert.Equal("response", actual);
        }

        [Fact]
        public void SendAndReceiveTimeout()
        {
            var latch = new CountdownEvent(1);

            Template.ReceiveTimeout = 1;
            Template.SendTimeout = 30000;
            Template.ThrowExceptionOnLateReply = true;

            var handler = new LateReplierMessageHandler(latch);
            var chanMock = new Mock<ISubscribableChannel>();
            var channel = chanMock.Object;
            chanMock.Setup(
                chan => chan.Send(It.IsAny<IMessage>(), It.Is<int>(i => i == 30000)))
                .Callback<IMessage, int>((m, t) => { Task.Run(() => handler.HandleMessage(m)); })
                .Returns(true);
            var result = Template.ConvertSendAndReceive<string>(channel, "request");
            Assert.Null(result);
            Assert.True(latch.Wait(10000));
            Assert.Null(handler.Failure);

            chanMock.Verify(chan => chan.Send(It.IsAny<IMessage>(), It.Is<int>(i => i == 30000)));
        }

        [Fact]
        public async Task SendAndReceiveAsyncTimeout()
        {
            var latch = new CountdownEvent(1);

            Template.ReceiveTimeout = 1;
            Template.SendTimeout = 30000;
            Template.ThrowExceptionOnLateReply = true;

            var handler = new LateReplierMessageHandler(latch);
            var chanMock = new Mock<ISubscribableChannel>();
            var channel = chanMock.Object;
            chanMock.Setup(
                chan => chan.SendAsync(It.IsAny<IMessage>(), It.Is<CancellationToken>(t => t.IsCancellationRequested == false)))
                .Callback<IMessage, CancellationToken>((m, t) => { Task.Run(() => handler.HandleMessage(m)); })
                .Returns(new ValueTask<bool>(true));
            var result = await Template.ConvertSendAndReceiveAsync<string>(channel, "request");
            Assert.Null(result);
            Assert.True(latch.Wait(10000));
            Assert.Null(handler.Failure);

            chanMock.Verify(chan => chan.SendAsync(It.IsAny<IMessage>(), It.Is<CancellationToken>(t => t.IsCancellationRequested == false)));
        }

        [Fact]
        public void SendAndReceiveVariableTimeout()
        {
            var latch = new CountdownEvent(1);
            Template.ReceiveTimeout = 10000;
            Template.SendTimeout = 20000;
            Template.ThrowExceptionOnLateReply = true;

            var handler = new LateReplierMessageHandler(latch);
            var chanMock = new Mock<ISubscribableChannel>();
            var channel = chanMock.Object;
            chanMock.Setup(
                chan => chan.Send(It.IsAny<IMessage>(), It.IsAny<int>()))
                .Callback<IMessage, int>((m, t) => { Task.Run(() => handler.HandleMessage(m)); })
                .Returns(true);

            var message = MessageBuilder.WithPayload("request")
                    .SetHeader(GenericMessagingTemplate.DEFAULT_SEND_TIMEOUT_HEADER, 30000)
                    .SetHeader(GenericMessagingTemplate.DEFAULT_RECEIVE_TIMEOUT_HEADER, 1)
                    .Build();

            var result = Template.SendAndReceive(channel, message);
            Assert.Null(result);
            Assert.True(latch.Wait(10000));
            Assert.Null(handler.Failure);

            chanMock.Verify(chan => chan.Send(It.IsAny<IMessage>(), It.Is<int>(i => i == 30000)));
        }

        [Fact]
        public async Task SendAndReceiveAsyncVariableTimeout()
        {
            var latch = new CountdownEvent(1);
            Template.ReceiveTimeout = 10000;
            Template.SendTimeout = 20000;
            Template.ThrowExceptionOnLateReply = true;

            var handler = new LateReplierMessageHandler(latch);
            var chanMock = new Mock<ISubscribableChannel>();
            var channel = chanMock.Object;
            chanMock.Setup(
                chan => chan.SendAsync(It.IsAny<IMessage>(), It.Is<CancellationToken>(t => t.IsCancellationRequested == false)))
                .Callback<IMessage, CancellationToken>((m, t) => { Task.Run(() => handler.HandleMessage(m)); })
                .Returns(new ValueTask<bool>(true));

            var message = MessageBuilder.WithPayload("request")
                    .SetHeader(GenericMessagingTemplate.DEFAULT_SEND_TIMEOUT_HEADER, 30000)
                    .SetHeader(GenericMessagingTemplate.DEFAULT_RECEIVE_TIMEOUT_HEADER, 1)
                    .Build();

            var result = await Template.SendAndReceiveAsync(channel, message);
            Assert.Null(result);
            Assert.True(latch.Wait(10000));
            Assert.Null(handler.Failure);

            chanMock.Verify(chan => chan.SendAsync(It.IsAny<IMessage>(), It.Is<CancellationToken>(t => t.IsCancellationRequested == false)));
        }

        [Fact]
        public void SendAndReceiveVariableTimeoutCustomHeaders()
        {
            var latch = new CountdownEvent(1);
            Template.ReceiveTimeout = 10000;
            Template.SendTimeout = 20000;
            Template.ThrowExceptionOnLateReply = true;
            Template.SendTimeoutHeader = "sto";
            Template.ReceiveTimeoutHeader = "rto";

            var handler = new LateReplierMessageHandler(latch);
            var chanMock = new Mock<ISubscribableChannel>();
            var channel = chanMock.Object;
            chanMock.Setup(
                chan => chan.Send(It.IsAny<IMessage>(), It.IsAny<int>()))
                .Callback<IMessage, int>((m, t) => { Task.Run(() => handler.HandleMessage(m)); })
                .Returns(true);

            var message = MessageBuilder.WithPayload("request")
                    .SetHeader("sto", 30000)
                    .SetHeader("rto", 1)
                    .Build();

            var result = Template.SendAndReceive(channel, message);
            Assert.Null(result);
            Assert.True(latch.Wait(10000));
            Assert.Null(handler.Failure);

            chanMock.Verify(chan => chan.Send(It.IsAny<IMessage>(), It.Is<int>(i => i == 30000)));
        }

        [Fact]
        public async Task SendAndReceiveAsyncVariableTimeoutCustomHeaders()
        {
            var latch = new CountdownEvent(1);
            Template.ReceiveTimeout = 10000;
            Template.SendTimeout = 20000;
            Template.ThrowExceptionOnLateReply = true;
            Template.SendTimeoutHeader = "sto";
            Template.ReceiveTimeoutHeader = "rto";

            var handler = new LateReplierMessageHandler(latch);
            var chanMock = new Mock<ISubscribableChannel>();
            var channel = chanMock.Object;
            chanMock.Setup(
                chan => chan.SendAsync(It.IsAny<IMessage>(), It.Is<CancellationToken>(t => t.IsCancellationRequested == false)))
                .Callback<IMessage, CancellationToken>((m, t) => { Task.Run(() => handler.HandleMessage(m)); })
                .Returns(new ValueTask<bool>(true));

            var message = MessageBuilder.WithPayload("request")
                    .SetHeader("sto", 30000)
                    .SetHeader("rto", 1)
                    .Build();

            var result = await Template.SendAndReceiveAsync(channel, message);
            Assert.Null(result);
            Assert.True(latch.Wait(10000));
            Assert.Null(handler.Failure);

            chanMock.Verify(chan => chan.SendAsync(It.IsAny<IMessage>(), It.Is<CancellationToken>(t => t.IsCancellationRequested == false)));
        }

        internal class LateReplierMessageHandler : IMessageHandler
        {
            public CountdownEvent Latch;
            public Exception Failure;

            public LateReplierMessageHandler(CountdownEvent latch)
            {
                Latch = latch;
            }

            public void HandleMessage(IMessage message)
            {
                try
                {
                    Thread.Sleep(1000);
                    var replyChannel = (IMessageChannel)message.Headers.ReplyChannel;
                    replyChannel.Send(new GenericMessage<string>("response"));
                    Failure = new InvalidOperationException("Expected exception");
                }
                catch (MessageDeliveryException ex)
                {
                    var expected = "Reply message received but the receiving thread has exited due to a timeout";
                    var actual = ex.Message;
                    if (!expected.Equals(actual))
                    {
                        Failure = new InvalidOperationException("Unexpected error: '" + actual + "'");
                    }
                }
                catch (Exception e)
                {
                    Failure = e;
                }
                finally
                {
                    Latch.Signal();
                }

                return;
            }
        }

        internal class SendAndReceiveTestHandler : IMessageHandler
        {
            public void HandleMessage(IMessage message)
            {
                var replyChannel = (IMessageChannel)message.Headers.ReplyChannel;
                replyChannel.Send(new GenericMessage<string>("response"));
                return;
            }
        }

        internal class TestDestinationResolver : IDestinationResolver<IMessageChannel>
        {
            private readonly GenericMessagingTemplateTest test;

            public TestDestinationResolver(GenericMessagingTemplateTest test)
            {
                this.test = test;
            }

            public IMessageChannel ResolveDestination(string name)
            {
                return test.MessageChannel;
            }

            object IDestinationResolver.ResolveDestination(string name)
            {
                return ResolveDestination(name);
            }
        }
    }
}
