// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Moq;
using System;
using System.Threading.Tasks;
using Xunit;
using static Steeltoe.Messaging.Support.Test.TaskSchedulerSubscribableChannelTest;

namespace Steeltoe.Messaging.Support.Test
{
    public class TaskSchedulerSubscribableChannelWriterTest
    {
        internal readonly TaskSchedulerSubscribableChannel _channel;
        internal readonly object _payload;
        internal readonly IMessage _message;

        internal IMessageHandler _handler;

        public TaskSchedulerSubscribableChannelWriterTest()
        {
            _channel = new TaskSchedulerSubscribableChannel();
            _payload = new object();
            _message = MessageBuilder.WithPayload(_payload).Build();
        }

        [Fact]
        public async void MessageMustNotBeNull()
        {
            Exception exception = null;
            try
            {
                await _channel.Writer.WriteAsync(null);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            Assert.NotNull(exception);
            Assert.Contains("message", exception.Message);
        }

        [Fact]
        public async ValueTask WriteAsyncNoInterceptors()
        {
            var mock = new Mock<IMessageHandler>();
            _handler = mock.Object;
            _channel.Subscribe(_handler);
            await _channel.Writer.WriteAsync(_message);
            mock.Verify(h => h.HandleMessage(_message));
        }

        [Fact]
        public async ValueTask WriteAsyncWithoutScheduler()
        {
            var mock = new Mock<IMessageHandler>();
            _handler = mock.Object;
            var interceptor = new BeforeHandleInterceptor();
            _channel.AddInterceptor(interceptor);
            _channel.Subscribe(_handler);
            await _channel.Writer.WriteAsync(_message);
            mock.Verify(h => h.HandleMessage(_message));
            Assert.Equal(1, interceptor.Counter);
            Assert.True(interceptor.WasAfterHandledInvoked);
        }

        [Fact]
        public async ValueTask WriteAsyncWithScheduler()
        {
            var mock = new Mock<IMessageHandler>();
            _handler = mock.Object;
            var interceptor = new BeforeHandleInterceptor();
            var scheduler = new TestScheduler();
            var testChannel = new TaskSchedulerSubscribableChannel(scheduler);
            testChannel.AddInterceptor(interceptor);
            testChannel.Subscribe(_handler);
            await testChannel.Writer.WriteAsync(_message);
            Assert.True(scheduler.WasTaskScheduled);
            mock.Verify(h => h.HandleMessage(_message));
            Assert.Equal(1, interceptor.Counter);
            Assert.True(interceptor.WasAfterHandledInvoked);
        }

        [Fact]
        public async ValueTask SubscribeTwice()
        {
            var mock = new Mock<IMessageHandler>();
            _handler = mock.Object;
            Assert.True(_channel.Subscribe(_handler));
            Assert.False(_channel.Subscribe(_handler));
            await _channel.Writer.WriteAsync(_message);
            mock.Verify(h => h.HandleMessage(_message), Times.Once);
        }

        [Fact]
        public async void UnsubscribeTwice()
        {
            var mock = new Mock<IMessageHandler>();
            _handler = mock.Object;
            _channel.Subscribe(_handler);
            Assert.True(_channel.Unsubscribe(_handler));
            Assert.False(_channel.Unsubscribe(_handler));
            await _channel.Writer.WriteAsync(_message);
            mock.Verify(h => h.HandleMessage(_message), Times.Never);
        }

        [Fact]
        public async ValueTask FailurePropagates()
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
                await _channel.Writer.WriteAsync(_message);
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
        public async ValueTask ConcurrentModification()
        {
            var mock = new Mock<IMessageHandler>();
            _handler = mock.Object;
            var unsubscribeHandler = new UnsubscribeHandler(this);
            _channel.Subscribe(unsubscribeHandler);
            _channel.Subscribe(_handler);
            await _channel.Writer.WriteAsync(_message);
            mock.Verify(h => h.HandleMessage(_message), Times.Once);
        }

        [Fact]
        public async ValueTask InterceptorWithModifiedMessage()
        {
            var mock = new Mock<IMessageHandler>();
            _handler = mock.Object;
            var mock2 = new Mock<IMessage>();
            var expected = mock2.Object;

            var interceptor = new BeforeHandleInterceptor();
            interceptor.MessageToReturn = expected;
            _channel.AddInterceptor(interceptor);
            _channel.Subscribe(_handler);
            await _channel.Writer.WriteAsync(_message);
            mock.Verify(h => h.HandleMessage(expected), Times.Once);

            Assert.Equal(1, interceptor.Counter);
            Assert.True(interceptor.WasAfterHandledInvoked);
        }

        [Fact]
        public async ValueTask InterceptorWithNull()
        {
            var mock = new Mock<IMessageHandler>();
            _handler = mock.Object;
            var interceptor1 = new BeforeHandleInterceptor();
            var interceptor2 = new NullReturningBeforeHandleInterceptor();
            _channel.AddInterceptor(interceptor1);
            _channel.AddInterceptor(interceptor2);
            _channel.Subscribe(_handler);
            await _channel.Writer.WriteAsync(_message);
            mock.Verify(h => h.HandleMessage(_message), Times.Never);
            Assert.Equal(1, interceptor1.Counter);
            Assert.Equal(1, interceptor2.Counter);
            Assert.True(interceptor1.WasAfterHandledInvoked);
        }

        [Fact]
        public async ValueTask InterceptorWithException()
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
                await _channel.Writer.WriteAsync(_message);
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

        [Fact]
        public async ValueTask TestWaitToWriteAsync()
        {
            var mock = new Mock<IMessageHandler>();
            _handler = mock.Object;
            _channel.Subscribe(_handler);
            Assert.True(await _channel.Writer.WaitToWriteAsync());
            _channel.Unsubscribe(_handler);
            Assert.False(await _channel.Writer.WaitToWriteAsync());
        }

        [Fact]
        public void TestTryComplete()
        {
            var mock = new Mock<IMessageHandler>();
            _handler = mock.Object;
            _channel.Subscribe(_handler);
            Assert.False(_channel.Writer.TryComplete());
        }

        [Fact]
        public void TryWriteNoInterceptors()
        {
            var mock = new Mock<IMessageHandler>();
            _handler = mock.Object;
            _channel.Subscribe(_handler);
            Assert.True(_channel.Writer.TryWrite(_message));
            mock.Verify(h => h.HandleMessage(_message));
        }

        [Fact]
        public void TryWriteWithInterceptors()
        {
            var mock = new Mock<IMessageHandler>();
            _handler = mock.Object;
            var interceptor = new BeforeHandleInterceptor();
            _channel.AddInterceptor(interceptor);
            _channel.Subscribe(_handler);
            Assert.True(_channel.Writer.TryWrite(_message));
            mock.Verify(h => h.HandleMessage(_message));
            Assert.Equal(1, interceptor.Counter);
            Assert.True(interceptor.WasAfterHandledInvoked);
        }
    }
}
