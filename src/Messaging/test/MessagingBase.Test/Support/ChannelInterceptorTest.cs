// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace Steeltoe.Messaging.Support.Test
{
    public class ChannelInterceptorTest
    {
        private readonly TaskSchedulerSubscribableChannel channel;

        private readonly TestMessageHandler messageHandler;

        public ChannelInterceptorTest()
        {
            channel = new TaskSchedulerSubscribableChannel();
            messageHandler = new TestMessageHandler();
            channel.Subscribe(messageHandler);
        }

        [Fact]
        public void PreSendInterceptorReturningModifiedMessage()
        {
            var expected = new Mock<IMessage>().Object;
            var interceptor = new PreSendInterceptor
            {
                MessageToReturn = expected
            };
            channel.AddInterceptor(interceptor);
            channel.Send(MessageBuilder.WithPayload("test").Build());

            Assert.Single(messageHandler.Messages);
            var result = messageHandler.Messages[0];

            Assert.NotNull(result);
            Assert.Same(expected, result);
            Assert.True(interceptor.WasAfterCompletionInvoked);
        }

        [Fact]
        public void PreSendInterceptorReturningNull()
        {
            var interceptor1 = new PreSendInterceptor();
            var interceptor2 = new NullReturningPreSendInterceptor();
            channel.AddInterceptor(interceptor1);
            channel.AddInterceptor(interceptor2);
            var message = MessageBuilder.WithPayload("test").Build();
            channel.Send(message);

            Assert.Equal(1, interceptor1.Counter);
            Assert.Equal(1, interceptor2.Counter);
            Assert.Empty(messageHandler.Messages);
            Assert.True(interceptor1.WasAfterCompletionInvoked);
            Assert.False(interceptor2.WasAfterCompletionInvoked);
        }

        [Fact]
        public void PostSendInterceptorMessageWasSent()
        {
            var interceptor = new PostSendInterceptorMessageWasSentChannelInterceptor(channel);
            channel.AddInterceptor(interceptor);

            channel.Send(MessageBuilder.WithPayload("test").Build());
            Assert.True(interceptor.PreSendInvoked);
            Assert.True(interceptor.CompletionInvoked);
        }

        [Fact]
        public void PostSendInterceptorMessageWasNotSent()
        {
            AbstractMessageChannel testChannel = new PostSendInterceptorMessageWasNotSentChannel();
            var interceptor = new PostSendInterceptorMessageWasNotSentInterceptor(testChannel);

            testChannel.AddInterceptor(interceptor);

            testChannel.Send(MessageBuilder.WithPayload("test").Build());
            Assert.True(interceptor.PreSendInvoked);
            Assert.True(interceptor.CompletionInvoked);
        }

        [Fact]
        public void AfterCompletionWithSendException()
        {
            AbstractMessageChannel testChannel = new AfterCompletionWithSendExceptionChannel();

            var interceptor1 = new PreSendInterceptor();
            var interceptor2 = new PreSendInterceptor();
            testChannel.AddInterceptor(interceptor1);
            testChannel.AddInterceptor(interceptor2);
            try
            {
                testChannel.Send(MessageBuilder.WithPayload("test").Build());
            }
            catch (Exception ex)
            {
                Assert.Equal("Simulated exception", ex.InnerException.Message);
            }

            Assert.True(interceptor1.WasAfterCompletionInvoked);
            Assert.True(interceptor2.WasAfterCompletionInvoked);
        }

        [Fact]
        public void AfterCompletionWithPreSendException()
        {
            var interceptor1 = new PreSendInterceptor();
            var interceptor2 = new PreSendInterceptor
            {
                ExceptionToRaise = new Exception("Simulated exception")
            };
            channel.AddInterceptor(interceptor1);
            channel.AddInterceptor(interceptor2);
            try
            {
                channel.Send(MessageBuilder.WithPayload("test").Build());
            }
            catch (Exception ex)
            {
                Assert.Equal("Simulated exception", ex.InnerException.Message);
            }

            Assert.True(interceptor1.WasAfterCompletionInvoked);
            Assert.False(interceptor2.WasAfterCompletionInvoked);
        }

        // internal class AfterCompletionWithSendExceptionChannel : AbstractMessageChannel
        // {
        //    protected override bool SendInternal(IMessage message, long timeout)
        //    {
        //        throw new Exception("Simulated exception");
        //    }

        // protected override Task<bool> SendInternalAsync(IMessage message, CancellationToken cancellation = default)
        //    {
        //        throw new NotImplementedException();
        //    }
        // }
        internal class AfterCompletionWithSendExceptionChannel : AbstractMessageChannel
        {
            public AfterCompletionWithSendExceptionChannel()
            {
                Writer = new AfterCompletionWithSendExceptionChannelWriter(this);
            }

            protected override bool DoSendInternal(IMessage message, CancellationToken cancellationToken)
            {
                throw new Exception("Simulated exception");
            }
        }

        internal class AfterCompletionWithSendExceptionChannelWriter : AbstractMessageChannelWriter
        {
            public AfterCompletionWithSendExceptionChannelWriter(AbstractMessageChannel channel, ILogger logger = null)
                : base(channel, logger)
            {
            }
        }

        // internal class PostSendInterceptorMessageWasNotSentChannel : AbstractMessageChannel
        // {
        //    protected override bool SendInternal(IMessage message, long timeout)
        //    {
        //        return false;
        //    }

        // protected override Task<bool> SendInternalAsync(IMessage message, CancellationToken cancellation = default)
        //    {
        //        throw new NotImplementedException();
        //    }
        // }
        internal class PostSendInterceptorMessageWasNotSentChannel : AbstractMessageChannel
        {
            public PostSendInterceptorMessageWasNotSentChannel()
            {
                Writer = new PostSendInterceptorMessageWasNotSentChannelWriter(this);
            }

            protected override bool DoSendInternal(IMessage message, CancellationToken cancellationToken)
            {
                return false;
            }
        }

        internal class PostSendInterceptorMessageWasNotSentChannelWriter : AbstractMessageChannelWriter
        {
            public PostSendInterceptorMessageWasNotSentChannelWriter(AbstractMessageChannel channel, ILogger logger = null)
                : base(channel, logger)
            {
            }
        }

        internal class PostSendInterceptorMessageWasNotSentInterceptor : AbstractChannelInterceptor
        {
            public bool PreSendInvoked = false;
            public bool CompletionInvoked = false;
            public IMessageChannel _expectedChannel;

            public PostSendInterceptorMessageWasNotSentInterceptor(IMessageChannel expectedChannel)
            {
                _expectedChannel = expectedChannel;
            }

            public override void PostSend(IMessage message, IMessageChannel channel, bool sent)
            {
                AssertInput(message, channel, sent);
                PreSendInvoked = true;
                return;
            }

            public override void AfterSendCompletion(IMessage message, IMessageChannel channel, bool sent, Exception ex)
            {
                AssertInput(message, channel, sent);
                CompletionInvoked = true;
                return;
            }

            private void AssertInput(IMessage message, IMessageChannel channel, bool sent)
            {
                Assert.NotNull(message);
                Assert.NotNull(channel);
                Assert.Same(_expectedChannel, channel);
                Assert.False(sent);
            }
        }

        internal class PostSendInterceptorMessageWasSentChannelInterceptor : AbstractChannelInterceptor
        {
            public bool PreSendInvoked = false;
            public bool CompletionInvoked = false;
            public IMessageChannel _expectedChannel;

            public PostSendInterceptorMessageWasSentChannelInterceptor(IMessageChannel expectedChannel)
            {
                _expectedChannel = expectedChannel;
            }

            public override void PostSend(IMessage message, IMessageChannel channel, bool sent)
            {
                AssertInput(message, channel, sent);
                PreSendInvoked = true;
                return;
            }

            public override void AfterSendCompletion(IMessage message, IMessageChannel channel, bool sent, Exception ex)
            {
                AssertInput(message, channel, sent);
                CompletionInvoked = true;
                return;
            }

            private void AssertInput(IMessage message, IMessageChannel channel, bool sent)
            {
                Assert.NotNull(message);
                Assert.NotNull(channel);
                Assert.Same(_expectedChannel, channel);
                Assert.True(sent);
            }
        }

        internal class TestMessageHandler : IMessageHandler
        {
            private readonly List<IMessage> messages = new List<IMessage>();

            public List<IMessage> Messages
            {
                get { return messages; }
            }

            public string ServiceName { get; set; } = nameof(TestMessageHandler);

            public void HandleMessage(IMessage message)
            {
                messages.Add(message);
                return;
            }
        }

        internal class AbstractTestInterceptor : AbstractTaskSchedulerChannelInterceptor
        {
            private volatile int counter;

            private volatile bool afterCompletionInvoked;

            public int Counter
            {
                get { return counter; }
            }

            public bool WasAfterCompletionInvoked
            {
                get { return afterCompletionInvoked; }
            }

            public override IMessage PreSend(IMessage message, IMessageChannel channel)
            {
                Assert.NotNull(message);
                Interlocked.Increment(ref counter);
                return message;
            }

            public override void AfterSendCompletion(IMessage message, IMessageChannel channel, bool sent, Exception ex)
            {
                afterCompletionInvoked = true;
                return;
            }
        }

        internal class PreSendInterceptor : AbstractTestInterceptor
        {
            private IMessage messageToReturn;

            private Exception exceptionToRaise;

            public IMessage MessageToReturn
            {
                get { return messageToReturn; }
                set { messageToReturn = value; }
            }

            public Exception ExceptionToRaise
            {
                get { return exceptionToRaise; }
                set { exceptionToRaise = value; }
            }

            public override IMessage PreSend(IMessage message, IMessageChannel channel)
            {
                base.PreSend(message, channel);
                if (exceptionToRaise != null)
                {
                    throw exceptionToRaise;
                }

                return messageToReturn ?? message;
            }
        }

        internal class NullReturningPreSendInterceptor : AbstractTestInterceptor
        {
            public override IMessage PreSend(IMessage message, IMessageChannel channel)
            {
                base.PreSend(message, channel);
                return null;
            }
        }
    }
}
