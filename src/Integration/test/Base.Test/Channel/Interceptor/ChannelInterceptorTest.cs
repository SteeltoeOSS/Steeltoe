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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;
using Steeltoe.Messaging.Support;
using System;
using System.Threading;
using Xunit;

namespace Steeltoe.Integration.Channel.Interceptor.Test
{
    public class ChannelInterceptorTest
    {
        private readonly QueueChannel channel;
        private readonly IServiceProvider provider;

        public ChannelInterceptorTest()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IDestinationRegistry, DefaultDestinationRegistry>();
            services.AddSingleton<IDestinationResolver<IMessageChannel>, DefaultMessageChannelDestinationResolver>();
            services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            provider = services.BuildServiceProvider();
            channel = new QueueChannel(provider);
        }

        [Fact]
        public void TestPreSendInterceptorReturnsMessage()
        {
            var interceptor = new PreSendReturnsMessageInterceptor();
            channel.AddInterceptor(interceptor);
            channel.Send(new GenericMessage("test"));
            var result = channel.Receive(0);
            Assert.NotNull(result);
            Assert.Equal("test", result.Payload);
            Assert.Equal(1, result.Headers[typeof(PreSendReturnsMessageInterceptor).Name]);
            Assert.True(interceptor.AfterCompletionInvoked);
        }

        [Fact]
        public void TestPreSendInterceptorReturnsNull()
        {
            var interceptor = new PreSendReturnsNullInterceptor();
            channel.AddInterceptor(interceptor);
            IMessage message = new GenericMessage("test");
            channel.Send(message);
            Assert.Equal(1, interceptor.Counter);

            Assert.True(channel.RemoveInterceptor(interceptor));

            channel.Send(new GenericMessage("TEST"));
            Assert.Equal(1, interceptor.Counter);

            var result = channel.Receive(0);
            Assert.NotNull(result);
            Assert.Equal("TEST", result.Payload);
        }

        [Fact]
        public void TestPostSendInterceptorWithSentMessage()
        {
            var interceptor = new TestPostSendInterceptorWithSentMessageInterceptor();
            channel.AddInterceptor(interceptor);
            channel.Send(new GenericMessage("test"));
            Assert.True(interceptor.Invoked);
        }

        [Fact]
        public void TestPostSendInterceptorWithUnsentMessage()
        {
            var singleItemChannel = new QueueChannel(provider, 1);
            var interceptor = new TestPostSendInterceptorWithUnsentMessageInterceptor();
            singleItemChannel.AddInterceptor(interceptor);
            Assert.Equal(0, interceptor.InvokedCounter);
            Assert.Equal(0, interceptor.SentCounter);
            singleItemChannel.Send(new GenericMessage("test1"));
            Assert.Equal(1, interceptor.InvokedCounter);
            Assert.Equal(1, interceptor.SentCounter);
            singleItemChannel.Send(new GenericMessage("test2"), 0);
            Assert.Equal(2, interceptor.InvokedCounter);
            Assert.Equal(1, interceptor.SentCounter);
            Assert.NotNull(singleItemChannel.RemoveInterceptor(0));
            singleItemChannel.Send(new GenericMessage("test2"), 0);
            Assert.Equal(2, interceptor.InvokedCounter);
            Assert.Equal(1, interceptor.SentCounter);
        }

        [Fact]
        public void AfterCompletionWithSendException()
        {
            AbstractMessageChannel testChannel = new AfterCompletionWithSendExceptionChannel(provider);

            var interceptor1 = new AfterCompletionTestInterceptor();
            var interceptor2 = new AfterCompletionTestInterceptor();
            testChannel.AddInterceptor(interceptor1);
            testChannel.AddInterceptor(interceptor2);
            try
            {
                testChannel.Send(Support.MessageBuilder.WithPayload("test").Build());
            }
            catch (Exception ex)
            {
                Assert.Equal("Simulated exception", ex.InnerException.Message);
            }

            Assert.True(interceptor1.AfterCompletionInvoked);
            Assert.True(interceptor2.AfterCompletionInvoked);
        }

        [Fact]
        public void AfterCompletionWithPreSendException()
        {
            var interceptor1 = new AfterCompletionTestInterceptor();
            var interceptor2 = new AfterCompletionTestInterceptor();
            interceptor2.ExceptionToRaise = new Exception("Simulated exception");
            channel.AddInterceptor(interceptor1);
            channel.AddInterceptor(interceptor2);
            try
            {
                channel.Send(Support.MessageBuilder.WithPayload("test").Build());
            }
            catch (Exception ex)
            {
                Assert.Equal("Simulated exception", ex.InnerException.Message);
            }

            Assert.True(interceptor1.AfterCompletionInvoked);
            Assert.False(interceptor2.AfterCompletionInvoked);
        }

        [Fact]
        public void TestPreReceiveInterceptorReturnsTrue()
        {
            var interceptor = new PreReceiveReturnsTrueInterceptor();
            channel.AddInterceptor(interceptor);
            var message = new GenericMessage("test");
            channel.Send(message);
            var result = channel.Receive(0);
            Assert.Equal(1, interceptor.Counter);
            Assert.NotNull(result);
            Assert.True(interceptor.AfterCompletionInvoked);
        }

        [Fact]
        public void TestPreReceiveInterceptorReturnsFalse()
        {
            var interceptor = new PreReceiveReturnsFalseInterceptor();
            channel.AddInterceptor(interceptor);
            var message = new GenericMessage("test");
            channel.Send(message);
            var result = channel.Receive(0);
            Assert.Equal(1, interceptor.Counter);
            Assert.Null(result);
        }

        [Fact]
        public void TestPostReceiveInterceptor()
        {
            var interceptor = new TestPostReceiveInterceptorInterceptor();
            channel.AddInterceptor(interceptor);

            channel.Receive(0);
            Assert.Equal(0, interceptor.Counter);
            channel.Send(new GenericMessage("test"));
            var result = channel.Receive(0);
            Assert.NotNull(result);
            Assert.Equal(1, interceptor.Counter);
        }

        [Fact]
        public void AfterCompletionWithReceiveException()
        {
            var interceptor1 = new PreReceiveReturnsTrueInterceptor();
            var interceptor2 = new PreReceiveReturnsTrueInterceptor();
            interceptor2.ExceptionToRaise = new Exception("Simulated exception");
            channel.AddInterceptor(interceptor1);
            channel.AddInterceptor(interceptor2);

            try
            {
                channel.Receive(0);
            }
            catch (Exception ex)
            {
                Assert.Equal("Simulated exception", ex.Message);
            }

            Assert.True(interceptor1.AfterCompletionInvoked);
            Assert.False(interceptor2.AfterCompletionInvoked);
        }

        public class TestPostReceiveInterceptorInterceptor : AbstractChannelInterceptor
        {
            public int Counter;

            public override IMessage PostReceive(IMessage message, IMessageChannel channel)
            {
                Assert.NotNull(channel);
                Interlocked.Increment(ref Counter);
                return message;
            }
        }

        public class AfterCompletionWithSendExceptionChannel : AbstractMessageChannel
        {
            public AfterCompletionWithSendExceptionChannel(IServiceProvider serviceProvider, ILogger logger = null)
                : base(serviceProvider, logger)
            {
            }

            protected override bool DoSendInternal(IMessage message, CancellationToken cancellationToken)
            {
                throw new Exception("Simulated exception");
            }
        }

        public class TestPostSendInterceptorWithUnsentMessageInterceptor : AbstractChannelInterceptor
        {
            public int InvokedCounter;
            public int SentCounter;

            public override void PostSend(IMessage message, IMessageChannel channel, bool sent)
            {
                {
                    Assert.NotNull(message);
                    Assert.NotNull(channel);
                    if (sent)
                    {
                        Interlocked.Increment(ref SentCounter);
                    }

                    Interlocked.Increment(ref InvokedCounter);
                }
            }
        }

        private class TestPostSendInterceptorWithSentMessageInterceptor : AbstractChannelInterceptor
        {
            public bool Invoked;

            public override void PostSend(IMessage message, IMessageChannel channel, bool sent)
            {
                Assert.NotNull(message);
                Assert.NotNull(channel);
                Assert.True(sent);
                Invoked = true;
            }
        }

        private class PreSendReturnsMessageInterceptor : AbstractChannelInterceptor
        {
            public int Counter;
            public volatile bool AfterCompletionInvoked;

            public PreSendReturnsMessageInterceptor()
            {
            }

            public override IMessage PreSend(IMessage message, IMessageChannel channel)
            {
                Assert.NotNull(message);
                var value = Interlocked.Increment(ref Counter);
                return Support.MessageBuilder.FromMessage(message)
                        .SetHeader(GetType().Name, value)
                        .Build();
            }

            public override void AfterSendCompletion(IMessage message, IMessageChannel channel, bool sent, Exception ex)
            {
                AfterCompletionInvoked = true;
            }
        }

        private class PreSendReturnsNullInterceptor : AbstractChannelInterceptor
        {
            public int Counter;

            public PreSendReturnsNullInterceptor()
            {
            }

            public override IMessage PreSend(IMessage message, IMessageChannel channel)
            {
                Assert.NotNull(message);
                Interlocked.Increment(ref Counter);
                return null;
            }
        }

        private class AfterCompletionTestInterceptor : AbstractChannelInterceptor
        {
            public Exception ExceptionToRaise;
            public int Counter;
            public volatile bool AfterCompletionInvoked;

            public AfterCompletionTestInterceptor()
            {
            }

            public override IMessage PreSend(IMessage message, IMessageChannel channel)
            {
                Assert.NotNull(message);
                Interlocked.Increment(ref Counter);
                if (ExceptionToRaise != null)
                {
                    throw ExceptionToRaise;
                }

                return message;
            }

            public override void AfterSendCompletion(IMessage message, IMessageChannel channel, bool sent, Exception ex)
            {
                AfterCompletionInvoked = true;
            }
        }

        private class PreReceiveReturnsTrueInterceptor : AbstractChannelInterceptor
        {
            public Exception ExceptionToRaise;
            public int Counter;
            public volatile bool AfterCompletionInvoked;

            public PreReceiveReturnsTrueInterceptor()
            {
            }

            public override bool PreReceive(IMessageChannel channel)
            {
                Interlocked.Increment(ref Counter);
                if (ExceptionToRaise != null)
                {
                    throw ExceptionToRaise;
                }

                return true;
            }

            public override void AfterReceiveCompletion(IMessage message, IMessageChannel channel, Exception ex)
            {
                AfterCompletionInvoked = true;
            }
        }

        private class PreReceiveReturnsFalseInterceptor : AbstractChannelInterceptor
        {
            public int Counter;

            public PreReceiveReturnsFalseInterceptor()
            {
            }

            public override bool PreReceive(IMessageChannel channel)
            {
                Interlocked.Increment(ref Counter);
                return false;
            }
        }
    }
}
