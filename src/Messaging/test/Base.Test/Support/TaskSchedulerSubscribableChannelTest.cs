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
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Messaging.Support.Test
{
    public class TaskSchedulerSubscribableChannelTest
    {
        internal readonly TaskSchedulerSubscribableChannel _channel;
        internal readonly object _payload;
        internal readonly IMessage<object> _message;

        internal IMessageHandler _handler;

        public TaskSchedulerSubscribableChannelTest()
        {
            _channel = new TaskSchedulerSubscribableChannel();
            _payload = new object();
            _message = MessageBuilder<object>.WithPayload(_payload).Build();
        }

        [Fact]
        public void MessageMustNotBeNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => _channel.Send(null));
            Assert.Contains("message", ex.Message);
        }

        [Fact]
        public void SendNoInterceptors()
        {
            var mock = new Mock<IMessageHandler>();
            _handler = mock.Object;
            _channel.Subscribe(_handler);
            _channel.Send(_message);
            mock.Verify(h => h.HandleMessage(_message));
        }

        [Fact]
        public void SendWithoutScheduler()
        {
            var mock = new Mock<IMessageHandler>();
            _handler = mock.Object;
            var interceptor = new BeforeHandleInterceptor();
            _channel.AddInterceptor(interceptor);
            _channel.Subscribe(_handler);
            _channel.Send(_message);
            mock.Verify(h => h.HandleMessage(_message));
            Assert.Equal(1, interceptor.Counter);
            Assert.True(interceptor.WasAfterHandledInvoked);
        }

        [Fact]
        public void SendWithScheduler()
        {
            var mock = new Mock<IMessageHandler>();
            _handler = mock.Object;
            var interceptor = new BeforeHandleInterceptor();
            var scheduler = new TestScheduler();
            var testChannel = new TaskSchedulerSubscribableChannel(scheduler);
            testChannel.AddInterceptor(interceptor);
            testChannel.Subscribe(_handler);
            testChannel.Send(_message);
            Assert.True(scheduler.WasTaskScheduled);
            mock.Verify(h => h.HandleMessage(_message));
            Assert.Equal(1, interceptor.Counter);
            Assert.True(interceptor.WasAfterHandledInvoked);
        }

        [Fact]
        public void SubscribeTwice()
        {
            var mock = new Mock<IMessageHandler>();
            _handler = mock.Object;
            Assert.True(_channel.Subscribe(_handler));
            Assert.False(_channel.Subscribe(_handler));
            _channel.Send(_message);
            mock.Verify(h => h.HandleMessage(_message), Times.Once);
        }

        [Fact]
        public void UnsubscribeTwice()
        {
            var mock = new Mock<IMessageHandler>();
            _handler = mock.Object;
            _channel.Subscribe(_handler);
            Assert.True(_channel.Unsubscribe(_handler));
            Assert.False(_channel.Unsubscribe(_handler));
            _channel.Send(_message);
            mock.Verify(h => h.HandleMessage(_message), Times.Never);
        }

        [Fact]
        public void FailurePropagates()
        {
            var ex = new Exception("My exception");
            var mock = new Mock<IMessageHandler>();
            mock.Setup(h => h.HandleMessage(_message)).Throws(ex);
            _handler = mock.Object;

            var mock2 = new Mock<IMessageHandler>();
            var secondHandler = mock2.Object;

            _channel.Subscribe(_handler);
            _channel.Subscribe(secondHandler);
            var exceptionThrown = false;
            try
            {
                _channel.Send(_message);
            }
            catch (MessageDeliveryException actualException)
            {
                exceptionThrown = true;
                Assert.Equal(ex, actualException.InnerException);
                Assert.Contains("My exception", actualException.InnerException.Message);
            }

            Assert.True(exceptionThrown);
            mock2.Verify(h => h.HandleMessage(_message), Times.Never);
        }

        [Fact]
        public void ConcurrentModification()
        {
            var mock = new Mock<IMessageHandler>();
            _handler = mock.Object;
            var unsubscribeHandler = new UnsubscribeHandler(this);
            _channel.Subscribe(unsubscribeHandler);
            _channel.Subscribe(_handler);
            _channel.Send(_message);
            mock.Verify(h => h.HandleMessage(_message), Times.Once);
        }

        [Fact]
        public void InterceptorWithModifiedMessage()
        {
            var mock = new Mock<IMessageHandler>();
            _handler = mock.Object;
            var mock2 = new Mock<IMessage>();
            var expected = mock2.Object;

            var interceptor = new BeforeHandleInterceptor();
            interceptor.MessageToReturn = expected;
            _channel.AddInterceptor(interceptor);
            _channel.Subscribe(_handler);
            _channel.Send(_message);
            mock.Verify(h => h.HandleMessage(expected), Times.Once);

            Assert.Equal(1, interceptor.Counter);
            Assert.True(interceptor.WasAfterHandledInvoked);
        }

        [Fact]
        public void InterceptorWithNull()
        {
            var mock = new Mock<IMessageHandler>();
            _handler = mock.Object;
            var interceptor1 = new BeforeHandleInterceptor();
            var interceptor2 = new NullReturningBeforeHandleInterceptor();
            _channel.AddInterceptor(interceptor1);
            _channel.AddInterceptor(interceptor2);
            _channel.Subscribe(_handler);
            _channel.Send(_message);
            mock.Verify(h => h.HandleMessage(_message), Times.Never);
            Assert.Equal(1, interceptor1.Counter);
            Assert.Equal(1, interceptor2.Counter);
            Assert.True(interceptor1.WasAfterHandledInvoked);
        }

        [Fact]
        public void InterceptorWithException()
        {
            var expected = new Exception("Fake exception");
            var mock = new Mock<IMessageHandler>();
            mock.Setup(h => h.HandleMessage(_message)).Throws(expected);
            _handler = mock.Object;

            var interceptor = new BeforeHandleInterceptor();
            _channel.AddInterceptor(interceptor);
            _channel.Subscribe(_handler);
            var exceptionThrown = false;
            try
            {
                _channel.Send(_message);
            }
            catch (MessageDeliveryException actual)
            {
                exceptionThrown = true;
                Assert.Same(expected, actual.InnerException);
            }

            Assert.True(exceptionThrown);
            mock.Verify(h => h.HandleMessage(_message), Times.Once);
            Assert.Equal(1, interceptor.Counter);
            Assert.True(interceptor.WasAfterHandledInvoked);
        }

        internal class UnsubscribeHandler : IMessageHandler
        {
            private TaskSchedulerSubscribableChannelTest _test;
            private TaskSchedulerSubscribableChannelWriterTest _test2;

            public UnsubscribeHandler(TaskSchedulerSubscribableChannelTest test)
            {
                _test = test;
            }

            public UnsubscribeHandler(TaskSchedulerSubscribableChannelWriterTest test)
            {
                _test2 = test;
            }

            public void HandleMessage(IMessage message)
            {
                _test?._channel.Unsubscribe(_test._handler);
                _test2?._channel.Unsubscribe(_test2._handler);
                return;
            }
        }

        internal class TestScheduler : TaskScheduler
        {
            public bool WasTaskScheduled = false;

            protected override IEnumerable<Task> GetScheduledTasks()
            {
                throw new NotImplementedException();
            }

            protected override void QueueTask(Task task)
            {
                WasTaskScheduled = true;
                TryExecuteTask(task);
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                WasTaskScheduled = true;
                TryExecuteTask(task);
                return true;
            }
        }

        internal class AbstractTestInterceptor : AbstractTaskSchedulerChannelInterceptor, IChannelInterceptor
        {
            private volatile int counter;

            private volatile bool afterHandledInvoked;

            public int Counter
            {
                get { return counter; }
            }

            public bool WasAfterHandledInvoked
            {
                get { return afterHandledInvoked; }
            }

            public override IMessage BeforeHandled(IMessage message, IMessageChannel channel, IMessageHandler handler)
            {
                Assert.NotNull(message);
                Interlocked.Increment(ref counter);
                return message;
            }

            public override void AfterMessageHandled(IMessage message, IMessageChannel channel, IMessageHandler handler, Exception ex)
            {
                afterHandledInvoked = true;
                return;
            }
        }

        internal class BeforeHandleInterceptor : AbstractTestInterceptor
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

            public override IMessage BeforeHandled(IMessage message, IMessageChannel channel, IMessageHandler handler)
            {
                base.BeforeHandled(message, channel, handler);
                if (exceptionToRaise != null)
                {
                    throw exceptionToRaise;
                }

                return messageToReturn != null ? messageToReturn : message;
            }
        }

        internal class NullReturningBeforeHandleInterceptor : AbstractTestInterceptor
        {
            public override IMessage BeforeHandled(IMessage message, IMessageChannel channel, IMessageHandler handler)
            {
                base.BeforeHandled(message, channel, handler);
                return null;
            }
        }
    }
}
