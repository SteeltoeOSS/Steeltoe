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

using Microsoft.Extensions.DependencyInjection;
using Moq;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Integration.Dispatcher.Test
{
    public class BroadcastingDispatcherTest
    {
        private readonly Mock<IMessage> messageMock = new Mock<IMessage>();

        private readonly Mock<IMessageHandler> targetMock1 = new Mock<IMessageHandler>();

        private readonly Mock<IMessageHandler> targetMock2 = new Mock<IMessageHandler>();

        private readonly Mock<IMessageHandler> targetMock3 = new Mock<IMessageHandler>();

        private readonly IServiceProvider provider;

        public BroadcastingDispatcherTest()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
            provider = services.BuildServiceProvider();
        }

        [Fact]
        public void SingleTargetWithoutTaskExecutor()
        {
            var dispatcher = new BroadcastingDispatcher(provider);
            dispatcher.AddHandler(targetMock1.Object);
            dispatcher.Dispatch(messageMock.Object);
            targetMock1.Verify((h) => h.HandleMessage(messageMock.Object));
        }

        [Fact]
        public void SingleTargetWithTaskExecutor()
        {
            var latch = new CountdownEvent(1);
            targetMock1.Setup((h) => h.HandleMessage(messageMock.Object)).Callback(() => latch.Signal());
            var dispatcher = new BroadcastingDispatcher(provider, TaskScheduler.Default);
            dispatcher.AddHandler(targetMock1.Object);
            dispatcher.Dispatch(messageMock.Object);
            Assert.True(latch.Wait(3000));
            targetMock1.Verify((h) => h.HandleMessage(messageMock.Object));
        }

        [Fact]
        public void MultipleTargetsWithoutTaskExecutor()
        {
            var dispatcher = new BroadcastingDispatcher(provider);
            dispatcher.AddHandler(targetMock1.Object);
            dispatcher.AddHandler(targetMock2.Object);
            dispatcher.AddHandler(targetMock3.Object);
            dispatcher.Dispatch(messageMock.Object);
            targetMock1.Verify((h) => h.HandleMessage(messageMock.Object));
            targetMock2.Verify((h) => h.HandleMessage(messageMock.Object));
            targetMock3.Verify((h) => h.HandleMessage(messageMock.Object));
        }

        [Fact]
        public void MultipleTargetsWithTaskExecutor()
        {
            var latch = new CountdownEvent(3);
            targetMock1.Setup((h) => h.HandleMessage(messageMock.Object)).Callback(() => latch.Signal());
            targetMock2.Setup((h) => h.HandleMessage(messageMock.Object)).Callback(() => latch.Signal());
            targetMock3.Setup((h) => h.HandleMessage(messageMock.Object)).Callback(() => latch.Signal());
            var dispatcher = new BroadcastingDispatcher(provider, TaskScheduler.Default);
            dispatcher.AddHandler(targetMock1.Object);
            dispatcher.AddHandler(targetMock2.Object);
            dispatcher.AddHandler(targetMock3.Object);
            dispatcher.Dispatch(messageMock.Object);
            Assert.True(latch.Wait(3000));
            targetMock1.Verify((h) => h.HandleMessage(messageMock.Object));
            targetMock2.Verify((h) => h.HandleMessage(messageMock.Object));
            targetMock3.Verify((h) => h.HandleMessage(messageMock.Object));
        }

        [Fact]
        public void MultipleTargetsPartialFailureFirst()
        {
            var latch = new CountdownEvent(2);
            targetMock1.Setup((h) => h.HandleMessage(messageMock.Object)).Callback(() => latch.Signal());
            targetMock2.Setup((h) => h.HandleMessage(messageMock.Object)).Callback(() => latch.Signal());
            targetMock3.Setup((h) => h.HandleMessage(messageMock.Object)).Callback(() => latch.Signal());
            var dispatcher = new BroadcastingDispatcher(provider, new PartialFailingTaskScheduler(false, true, true));
            dispatcher.AddHandler(targetMock1.Object);
            dispatcher.AddHandler(targetMock2.Object);
            dispatcher.AddHandler(targetMock3.Object);
            dispatcher.Dispatch(messageMock.Object);
            Assert.True(latch.Wait(3000));
            targetMock1.Verify((h) => h.HandleMessage(messageMock.Object), Times.Never());
            targetMock2.Verify((h) => h.HandleMessage(messageMock.Object));
            targetMock3.Verify((h) => h.HandleMessage(messageMock.Object));
        }

        [Fact]
        public void MultipleTargetsPartialFailureMiddle()
        {
            var latch = new CountdownEvent(2);
            targetMock1.Setup((h) => h.HandleMessage(messageMock.Object)).Callback(() => latch.Signal());
            targetMock2.Setup((h) => h.HandleMessage(messageMock.Object)).Callback(() => latch.Signal());
            targetMock3.Setup((h) => h.HandleMessage(messageMock.Object)).Callback(() => latch.Signal());
            var dispatcher = new BroadcastingDispatcher(provider, new PartialFailingTaskScheduler(true, false, true));
            dispatcher.AddHandler(targetMock1.Object);
            dispatcher.AddHandler(targetMock2.Object);
            dispatcher.AddHandler(targetMock3.Object);
            dispatcher.Dispatch(messageMock.Object);
            Assert.True(latch.Wait(3000));
            targetMock1.Verify((h) => h.HandleMessage(messageMock.Object));
            targetMock2.Verify((h) => h.HandleMessage(messageMock.Object), Times.Never());
            targetMock3.Verify((h) => h.HandleMessage(messageMock.Object));
        }

        [Fact]
        public void MultipleTargetsPartialFailureLast()
        {
            var latch = new CountdownEvent(2);
            targetMock1.Setup((h) => h.HandleMessage(messageMock.Object)).Callback(() => latch.Signal());
            targetMock2.Setup((h) => h.HandleMessage(messageMock.Object)).Callback(() => latch.Signal());
            targetMock3.Setup((h) => h.HandleMessage(messageMock.Object)).Callback(() => latch.Signal());
            var dispatcher = new BroadcastingDispatcher(provider, new PartialFailingTaskScheduler(true, true, false));
            dispatcher.AddHandler(targetMock1.Object);
            dispatcher.AddHandler(targetMock2.Object);
            dispatcher.AddHandler(targetMock3.Object);
            dispatcher.Dispatch(messageMock.Object);
            Assert.True(latch.Wait(3000));
            targetMock1.Verify((h) => h.HandleMessage(messageMock.Object));
            targetMock2.Verify((h) => h.HandleMessage(messageMock.Object));
            targetMock3.Verify((h) => h.HandleMessage(messageMock.Object), Times.Never());
        }

        [Fact]
        public void MultipleTargetsAllFail()
        {
            var dispatcher = new BroadcastingDispatcher(provider, new PartialFailingTaskScheduler(false, false, false));
            dispatcher.AddHandler(targetMock1.Object);
            dispatcher.AddHandler(targetMock2.Object);
            dispatcher.AddHandler(targetMock3.Object);
            dispatcher.Dispatch(messageMock.Object);
            targetMock1.Verify((h) => h.HandleMessage(messageMock.Object), Times.Never());
            targetMock2.Verify((h) => h.HandleMessage(messageMock.Object), Times.Never());
            targetMock3.Verify((h) => h.HandleMessage(messageMock.Object), Times.Never());
        }

        [Fact]
        public void NoDuplicateSubscription()
        {
            var dispatcher = new BroadcastingDispatcher(provider);
            dispatcher.AddHandler(targetMock1.Object);
            dispatcher.AddHandler(targetMock1.Object);
            dispatcher.AddHandler(targetMock1.Object);
            dispatcher.Dispatch(messageMock.Object);
            targetMock1.Verify((h) => h.HandleMessage(messageMock.Object));
        }

        [Fact]
        public void RemoveConsumerBeforeSend()
        {
            var dispatcher = new BroadcastingDispatcher(provider);
            dispatcher.AddHandler(targetMock1.Object);
            dispatcher.AddHandler(targetMock2.Object);
            dispatcher.AddHandler(targetMock3.Object);
            dispatcher.RemoveHandler(targetMock2.Object);
            dispatcher.Dispatch(messageMock.Object);
            targetMock1.Verify((h) => h.HandleMessage(messageMock.Object));
            targetMock2.Verify((h) => h.HandleMessage(messageMock.Object), Times.Never());
            targetMock3.Verify((h) => h.HandleMessage(messageMock.Object));
        }

        [Fact]
        public void RemoveConsumerBetweenSends()
        {
            var dispatcher = new BroadcastingDispatcher(provider);
            dispatcher.AddHandler(targetMock1.Object);
            dispatcher.AddHandler(targetMock2.Object);
            dispatcher.AddHandler(targetMock3.Object);
            dispatcher.Dispatch(messageMock.Object);
            dispatcher.RemoveHandler(targetMock2.Object);
            dispatcher.Dispatch(messageMock.Object);
            targetMock1.Verify((h) => h.HandleMessage(messageMock.Object), Times.Exactly(2));
            targetMock2.Verify((h) => h.HandleMessage(messageMock.Object), Times.Exactly(1));
            targetMock3.Verify((h) => h.HandleMessage(messageMock.Object), Times.Exactly(2));
        }

        [Fact]
        public void ApplySequenceDisabledByDefault()
        {
            var dispatcher = new BroadcastingDispatcher(provider);
            var messages = new ConcurrentQueue<IMessage>();
            var target1 = new MessageStoringTestEndpoint(messages);
            var target2 = new MessageStoringTestEndpoint(messages);
            dispatcher.AddHandler(target1);
            dispatcher.AddHandler(target2);
            dispatcher.Dispatch(new GenericMessage("test"));
            Assert.Equal(2, messages.Count);

            Assert.True(messages.TryDequeue(out var message));
            message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.SEQUENCE_NUMBER, out var value);
            Assert.Null(value);
            message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.SEQUENCE_SIZE, out value);
            Assert.Null(value);

            Assert.True(messages.TryDequeue(out message));
            message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.SEQUENCE_NUMBER, out value);
            Assert.Null(value);
            message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.SEQUENCE_SIZE, out value);
            Assert.Null(value);
        }

        [Fact]
        public void ApplySequenceEnabled()
        {
            var dispatcher = new BroadcastingDispatcher(provider);
            dispatcher.ApplySequence = true;
            var messages = new ConcurrentQueue<IMessage>();
            var target1 = new MessageStoringTestEndpoint(messages);
            var target2 = new MessageStoringTestEndpoint(messages);
            var target3 = new MessageStoringTestEndpoint(messages);
            dispatcher.AddHandler(target1);
            dispatcher.AddHandler(target2);
            dispatcher.AddHandler(target3);
            IMessage inputMessage = new GenericMessage("test");
            var originalId = inputMessage.Headers.Id;
            dispatcher.Dispatch(inputMessage);
            Assert.Equal(3, messages.Count);

            Assert.True(messages.TryDequeue(out var message));
            message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.SEQUENCE_NUMBER, out var value);
            Assert.Equal(1, value);
            message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.SEQUENCE_SIZE, out value);
            Assert.Equal(3, value);
            message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.CORRELATION_ID, out value);
            Assert.Equal(originalId, value);

            Assert.True(messages.TryDequeue(out message));
            message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.SEQUENCE_NUMBER, out value);
            Assert.Equal(2, value);
            message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.SEQUENCE_SIZE, out value);
            Assert.Equal(3, value);
            message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.CORRELATION_ID, out value);
            Assert.Equal(originalId, value);

            Assert.True(messages.TryDequeue(out message));
            message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.SEQUENCE_NUMBER, out value);
            Assert.Equal(3, value);
            message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.SEQUENCE_SIZE, out value);
            Assert.Equal(3, value);
            message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.CORRELATION_ID, out value);
            Assert.Equal(originalId, value);
        }

        [Fact]
        public void TestExceptionEnhancement()
        {
            var dispatcher = new BroadcastingDispatcher(provider);
            dispatcher.AddHandler(targetMock1.Object);
            targetMock1.Setup((h) => h.HandleMessage(messageMock.Object)).Throws(new MessagingException("Mock Exception"));

            try
            {
                dispatcher.Dispatch(messageMock.Object);
                throw new Exception("Expected Exception");
            }
            catch (MessagingException e)
            {
                Assert.Equal(messageMock.Object, e.FailedMessage);
            }
        }

        [Fact]
        public void TestNoExceptionEnhancement()
        {
            var dispatcher = new BroadcastingDispatcher(provider);
            dispatcher.AddHandler(targetMock1.Object);
            targetMock1.Object.HandleMessage(messageMock.Object);
            var dontReplaceThisMessage = Support.MessageBuilder.WithPayload("x").Build();
            targetMock1.Setup((h) => h.HandleMessage(messageMock.Object)).Throws(new MessagingException(dontReplaceThisMessage, "Mock Exception"));

            try
            {
                dispatcher.Dispatch(messageMock.Object);
                throw new Exception("Expected Exception");
            }
            catch (MessagingException e)
            {
                Assert.Equal(dontReplaceThisMessage, e.FailedMessage);
            }
        }

        [Fact]
        public void TestNoHandler()
        {
            var dispatcher = new BroadcastingDispatcher(provider);
            Assert.True(dispatcher.Dispatch(messageMock.Object));
        }

        [Fact]
        public void TestNoHandlerWithExecutor()
        {
            var dispatcher = new BroadcastingDispatcher(provider, TaskScheduler.Default);
            Assert.True(dispatcher.Dispatch(messageMock.Object));
        }

        [Fact]
        public void TestNoHandlerWithRequiredSubscriber()
        {
            var dispatcher = new BroadcastingDispatcher(provider, true);
            try
            {
                dispatcher.Dispatch(messageMock.Object);
                new Exception("Expected Exception");
            }
            catch (MessageDispatchingException exception)
            {
                Assert.Equal(messageMock.Object, exception.FailedMessage);
            }
        }

        [Fact]
        public void TestNoHandlerWithExecutorWithRequiredSubscriber()
        {
            var dispatcher = new BroadcastingDispatcher(provider, TaskScheduler.Default, true);
            try
            {
                dispatcher.Dispatch(messageMock.Object);
                new Exception("Expected Exception");
            }
            catch (MessageDispatchingException exception)
            {
                Assert.Equal(messageMock.Object, exception.FailedMessage);
            }
        }

        private class MessageStoringTestEndpoint : IMessageHandler
        {
            public ConcurrentQueue<IMessage> MessageList;

            public MessageStoringTestEndpoint(ConcurrentQueue<IMessage> messageList)
            {
                MessageList = messageList;
            }

            public void HandleMessage(IMessage message)
            {
                MessageList.Enqueue(message);
            }
        }

        private class PartialFailingTaskScheduler : TaskScheduler
        {
            private readonly bool[] failures;
            private int count = -1;

            public PartialFailingTaskScheduler(params bool[] failures)
            {
                this.failures = failures;
            }

            protected override IEnumerable<Task> GetScheduledTasks()
            {
                return new List<Task>();
            }

            protected override void QueueTask(Task task)
            {
                var val = Interlocked.Increment(ref count);
                if (val < failures.Length)
                {
                    if (failures[val])
                    {
                        TryExecuteTask(task);
                        return;
                    }
                }
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                return false;
            }
        }
    }
}
