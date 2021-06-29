// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
        internal MessageChannelTemplate _template;

        internal StubMessageChannel _messageChannel;

        public GenericMessagingTemplateTest()
        {
            _messageChannel = new StubMessageChannel();
            _template = new MessageChannelTemplate
            {
                DefaultSendDestination = _messageChannel,
                DestinationResolver = new TestDestinationResolver(this)
            };
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
                    .SetHeader(MessageChannelTemplate.DEFAULT_SEND_TIMEOUT_HEADER, 30000)
                    .SetHeader(MessageChannelTemplate.DEFAULT_RECEIVE_TIMEOUT_HEADER, 1)
                    .Build();

            _template.Send(channel, message);

            chanMock.Verify(chan => chan.Send(It.IsAny<IMessage>(), It.Is<int>(i => i == 30000)));
            Assert.NotNull(sent);
            Assert.False(sent.Headers.ContainsKey(MessageChannelTemplate.DEFAULT_SEND_TIMEOUT_HEADER));
            Assert.False(sent.Headers.ContainsKey(MessageChannelTemplate.DEFAULT_RECEIVE_TIMEOUT_HEADER));
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
                    .SetHeader(MessageChannelTemplate.DEFAULT_SEND_TIMEOUT_HEADER, 30000)
                    .SetHeader(MessageChannelTemplate.DEFAULT_RECEIVE_TIMEOUT_HEADER, 1)
                    .Build();

            await _template.SendAsync(channel, message);

            chanMock.Verify(chan => chan.SendAsync(It.IsAny<IMessage>(), It.Is<CancellationToken>(t => t.IsCancellationRequested == false)));
            Assert.NotNull(sent);
            Assert.False(sent.Headers.ContainsKey(MessageChannelTemplate.DEFAULT_SEND_TIMEOUT_HEADER));
            Assert.False(sent.Headers.ContainsKey(MessageChannelTemplate.DEFAULT_RECEIVE_TIMEOUT_HEADER));
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

            var accessor = new MessageHeaderAccessor
            {
                LeaveMutable = true
            };
            var message = Message.Create<string>("request", accessor.MessageHeaders);
            accessor.SetHeader(MessageChannelTemplate.DEFAULT_SEND_TIMEOUT_HEADER, 30000);
            await _template.SendAsync(channel, message);
            chanMock.Verify(chan => chan.SendAsync(It.IsAny<IMessage>(), It.Is<CancellationToken>(t => t.IsCancellationRequested == false)));
            Assert.NotNull(sent);
            Assert.False(sent.Headers.ContainsKey(MessageChannelTemplate.DEFAULT_SEND_TIMEOUT_HEADER));
            Assert.False(sent.Headers.ContainsKey(MessageChannelTemplate.DEFAULT_RECEIVE_TIMEOUT_HEADER));
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

            var accessor = new MessageHeaderAccessor
            {
                LeaveMutable = true
            };
            var message = Message.Create<string>("request", accessor.MessageHeaders);
            accessor.SetHeader(MessageChannelTemplate.DEFAULT_SEND_TIMEOUT_HEADER, 30000);
            _template.Send(channel, message);
            chanMock.Verify(chan => chan.Send(It.IsAny<IMessage>(), It.Is<int>(i => i == 30000)));
            Assert.NotNull(sent);
            Assert.False(sent.Headers.ContainsKey(MessageChannelTemplate.DEFAULT_SEND_TIMEOUT_HEADER));
            Assert.False(sent.Headers.ContainsKey(MessageChannelTemplate.DEFAULT_RECEIVE_TIMEOUT_HEADER));
        }

        [Fact]
        public void SendAndReceive()
        {
            var channel = new TaskSchedulerSubscribableChannel(TaskScheduler.Default);
            channel.Subscribe(new SendAndReceiveTestHandler());

            var actual = _template.ConvertSendAndReceive<string>(channel, "request");
            Assert.Equal("response", actual);
        }

        [Fact]
        public async Task SendAndReceiveAsync()
        {
            var channel = new TaskSchedulerSubscribableChannel(TaskScheduler.Default);
            channel.Subscribe(new SendAndReceiveTestHandler());

            var actual = await _template.ConvertSendAndReceiveAsync<string>(channel, "request");
            Assert.Equal("response", actual);
        }

        [Fact]
        public void SendAndReceiveTimeout()
        {
            var latch = new CountdownEvent(1);

            _template.ReceiveTimeout = 1;
            _template.SendTimeout = 30000;
            _template.ThrowExceptionOnLateReply = true;

            var handler = new LateReplierMessageHandler(latch);
            var chanMock = new Mock<ISubscribableChannel>();
            var channel = chanMock.Object;
            chanMock.Setup(
                chan => chan.Send(It.IsAny<IMessage>(), It.Is<int>(i => i == 30000)))
                .Callback<IMessage, int>((m, t) => { Task.Run(() => handler.HandleMessage(m)); })
                .Returns(true);
            var result = _template.ConvertSendAndReceive<string>(channel, "request");
            Assert.Null(result);
            Assert.True(latch.Wait(10000));
            Assert.Null(handler._failure);

            chanMock.Verify(chan => chan.Send(It.IsAny<IMessage>(), It.Is<int>(i => i == 30000)));
        }

        [Fact]
        public async Task SendAndReceiveAsyncTimeout()
        {
            var latch = new CountdownEvent(1);

            _template.ReceiveTimeout = 1;
            _template.SendTimeout = 30000;
            _template.ThrowExceptionOnLateReply = true;

            var handler = new LateReplierMessageHandler(latch);
            var chanMock = new Mock<ISubscribableChannel>();
            var channel = chanMock.Object;
            chanMock.Setup(
                chan => chan.SendAsync(It.IsAny<IMessage>(), It.Is<CancellationToken>(t => t.IsCancellationRequested == false)))
                .Callback<IMessage, CancellationToken>((m, t) => { Task.Run(() => handler.HandleMessage(m), t); })
                .Returns(new ValueTask<bool>(true));
            var result = await _template.ConvertSendAndReceiveAsync<string>(channel, "request");
            Assert.Null(result);
            Assert.True(latch.Wait(10000));
            Assert.Null(handler._failure);

            chanMock.Verify(chan => chan.SendAsync(It.IsAny<IMessage>(), It.Is<CancellationToken>(t => t.IsCancellationRequested == false)));
        }

        [Fact]
        public void SendAndReceiveVariableTimeout()
        {
            var latch = new CountdownEvent(1);
            _template.ReceiveTimeout = 10000;
            _template.SendTimeout = 20000;
            _template.ThrowExceptionOnLateReply = true;

            var handler = new LateReplierMessageHandler(latch);
            var chanMock = new Mock<ISubscribableChannel>();
            var channel = chanMock.Object;
            chanMock.Setup(
                chan => chan.Send(It.IsAny<IMessage>(), It.IsAny<int>()))
                .Callback<IMessage, int>((m, t) => { Task.Run(() => handler.HandleMessage(m)); })
                .Returns(true);

            var message = MessageBuilder.WithPayload("request")
                    .SetHeader(MessageChannelTemplate.DEFAULT_SEND_TIMEOUT_HEADER, 30000)
                    .SetHeader(MessageChannelTemplate.DEFAULT_RECEIVE_TIMEOUT_HEADER, 1)
                    .Build();

            var result = _template.SendAndReceive(channel, message);
            Assert.Null(result);
            Assert.True(latch.Wait(10000));
            Assert.Null(handler._failure);

            chanMock.Verify(chan => chan.Send(It.IsAny<IMessage>(), It.Is<int>(i => i == 30000)));
        }

        [Fact]
        public async Task SendAndReceiveAsyncVariableTimeout()
        {
            var latch = new CountdownEvent(1);
            _template.ReceiveTimeout = 10000;
            _template.SendTimeout = 20000;
            _template.ThrowExceptionOnLateReply = true;

            var handler = new LateReplierMessageHandler(latch);
            var chanMock = new Mock<ISubscribableChannel>();
            var channel = chanMock.Object;
            chanMock.Setup(
                chan => chan.SendAsync(It.IsAny<IMessage>(), It.Is<CancellationToken>(t => t.IsCancellationRequested == false)))
                .Callback<IMessage, CancellationToken>((m, t) => { Task.Run(() => handler.HandleMessage(m), t); })
                .Returns(new ValueTask<bool>(true));

            var message = MessageBuilder.WithPayload("request")
                    .SetHeader(MessageChannelTemplate.DEFAULT_SEND_TIMEOUT_HEADER, 30000)
                    .SetHeader(MessageChannelTemplate.DEFAULT_RECEIVE_TIMEOUT_HEADER, 1)
                    .Build();

            var result = await _template.SendAndReceiveAsync(channel, message);
            Assert.Null(result);
            Assert.True(latch.Wait(10000));
            Assert.Null(handler._failure);

            chanMock.Verify(chan => chan.SendAsync(It.IsAny<IMessage>(), It.Is<CancellationToken>(t => t.IsCancellationRequested == false)));
        }

        [Fact]
        public void SendAndReceiveVariableTimeoutCustomHeaders()
        {
            var latch = new CountdownEvent(1);
            _template.ReceiveTimeout = 10000;
            _template.SendTimeout = 20000;
            _template.ThrowExceptionOnLateReply = true;
            _template.SendTimeoutHeader = "sto";
            _template.ReceiveTimeoutHeader = "rto";

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

            var result = _template.SendAndReceive(channel, message);
            Assert.Null(result);
            Assert.True(latch.Wait(10000));
            Assert.Null(handler._failure);

            chanMock.Verify(chan => chan.Send(It.IsAny<IMessage>(), It.Is<int>(i => i == 30000)));
        }

        [Fact]
        public async Task SendAndReceiveAsyncVariableTimeoutCustomHeaders()
        {
            var latch = new CountdownEvent(1);
            _template.ReceiveTimeout = 10000;
            _template.SendTimeout = 20000;
            _template.ThrowExceptionOnLateReply = true;
            _template.SendTimeoutHeader = "sto";
            _template.ReceiveTimeoutHeader = "rto";

            var handler = new LateReplierMessageHandler(latch);
            var chanMock = new Mock<ISubscribableChannel>();
            var channel = chanMock.Object;
            chanMock.Setup(
                chan => chan.SendAsync(It.IsAny<IMessage>(), It.Is<CancellationToken>(t => t.IsCancellationRequested == false)))
                .Callback<IMessage, CancellationToken>((m, t) => { Task.Run(() => handler.HandleMessage(m), t); })
                .Returns(new ValueTask<bool>(true));

            var message = MessageBuilder.WithPayload("request")
                    .SetHeader("sto", 30000)
                    .SetHeader("rto", 1)
                    .Build();

            var result = await _template.SendAndReceiveAsync(channel, message);
            Assert.Null(result);
            Assert.True(latch.Wait(10000));
            Assert.Null(handler._failure);

            chanMock.Verify(chan => chan.SendAsync(It.IsAny<IMessage>(), It.Is<CancellationToken>(t => t.IsCancellationRequested == false)));
        }

        internal class LateReplierMessageHandler : IMessageHandler
        {
            public CountdownEvent _latch;
            public Exception _failure;

            public LateReplierMessageHandler(CountdownEvent latch)
            {
                _latch = latch;
            }

            public string ServiceName { get; set; } = nameof(LateReplierMessageHandler);

            public void HandleMessage(IMessage message)
            {
                try
                {
                    Thread.Sleep(1000);
                    var replyChannel = (IMessageChannel)message.Headers.ReplyChannel;
                    replyChannel.Send(Message.Create<string>("response"));
                    _failure = new InvalidOperationException("Expected exception");
                }
                catch (MessageDeliveryException ex)
                {
                    var expected = "Reply message received but the receiving thread has exited due to a timeout";
                    var actual = ex.Message;
                    if (!expected.Equals(actual))
                    {
                        _failure = new InvalidOperationException("Unexpected error: '" + actual + "'");
                    }
                }
                catch (Exception e)
                {
                    _failure = e;
                }
                finally
                {
                    _latch.Signal();
                }

                return;
            }
        }

        internal class SendAndReceiveTestHandler : IMessageHandler
        {
            public string ServiceName { get; set; } = nameof(SendAndReceiveTestHandler);

            public void HandleMessage(IMessage message)
            {
                var replyChannel = (IMessageChannel)message.Headers.ReplyChannel;
                replyChannel.Send(Message.Create<string>("response"));
                return;
            }
        }

        internal class TestDestinationResolver : IDestinationResolver<IMessageChannel>
        {
            private readonly GenericMessagingTemplateTest _test;

            public TestDestinationResolver(GenericMessagingTemplateTest test)
            {
                _test = test;
            }

            public IMessageChannel ResolveDestination(string name)
            {
                return _test._messageChannel;
            }

            object IDestinationResolver.ResolveDestination(string name)
            {
                return ResolveDestination(name);
            }
        }
    }
}
