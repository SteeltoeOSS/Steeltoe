// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Xunit;

namespace Steeltoe.Integration.Dispatcher.Test;

public class BroadcastingDispatcherTest
{
    private readonly Mock<IMessage> _messageMock = new();

    private readonly Mock<IMessageHandler> _targetMock1 = new();

    private readonly Mock<IMessageHandler> _targetMock2 = new();

    private readonly Mock<IMessageHandler> _targetMock3 = new();

    private readonly IServiceProvider _provider;

    public BroadcastingDispatcherTest()
    {
        var services = new ServiceCollection();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(configurationRoot);
        services.AddSingleton<IApplicationContext, GenericApplicationContext>();
        services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public void SingleTargetWithoutTaskExecutor()
    {
        var dispatcher = new BroadcastingDispatcher(_provider.GetService<IApplicationContext>());
        dispatcher.AddHandler(_targetMock1.Object);
        dispatcher.Dispatch(_messageMock.Object);
        _targetMock1.Verify(h => h.HandleMessage(_messageMock.Object));
    }

    [Fact]
    public void SingleTargetWithTaskExecutor()
    {
        var latch = new CountdownEvent(1);
        _targetMock1.Setup(h => h.HandleMessage(_messageMock.Object)).Callback(() => latch.Signal());
        var dispatcher = new BroadcastingDispatcher(_provider.GetService<IApplicationContext>(), TaskScheduler.Default);
        dispatcher.AddHandler(_targetMock1.Object);
        dispatcher.Dispatch(_messageMock.Object);
        Assert.True(latch.Wait(3000));
        _targetMock1.Verify(h => h.HandleMessage(_messageMock.Object));
    }

    [Fact]
    public void MultipleTargetsWithoutTaskExecutor()
    {
        var dispatcher = new BroadcastingDispatcher(_provider.GetService<IApplicationContext>());
        dispatcher.AddHandler(_targetMock1.Object);
        dispatcher.AddHandler(_targetMock2.Object);
        dispatcher.AddHandler(_targetMock3.Object);
        dispatcher.Dispatch(_messageMock.Object);
        _targetMock1.Verify(h => h.HandleMessage(_messageMock.Object));
        _targetMock2.Verify(h => h.HandleMessage(_messageMock.Object));
        _targetMock3.Verify(h => h.HandleMessage(_messageMock.Object));
    }

    [Fact]
    public void MultipleTargetsWithTaskExecutor()
    {
        var latch = new CountdownEvent(3);
        _targetMock1.Setup(h => h.HandleMessage(_messageMock.Object)).Callback(() => latch.Signal());
        _targetMock2.Setup(h => h.HandleMessage(_messageMock.Object)).Callback(() => latch.Signal());
        _targetMock3.Setup(h => h.HandleMessage(_messageMock.Object)).Callback(() => latch.Signal());
        var dispatcher = new BroadcastingDispatcher(_provider.GetService<IApplicationContext>(), TaskScheduler.Default);
        dispatcher.AddHandler(_targetMock1.Object);
        dispatcher.AddHandler(_targetMock2.Object);
        dispatcher.AddHandler(_targetMock3.Object);
        dispatcher.Dispatch(_messageMock.Object);
        Assert.True(latch.Wait(3000));
        _targetMock1.Verify(h => h.HandleMessage(_messageMock.Object));
        _targetMock2.Verify(h => h.HandleMessage(_messageMock.Object));
        _targetMock3.Verify(h => h.HandleMessage(_messageMock.Object));
    }

    [Fact]
    public void MultipleTargetsPartialFailureFirst()
    {
        var latch = new CountdownEvent(2);
        _targetMock1.Setup(h => h.HandleMessage(_messageMock.Object)).Callback(() => latch.Signal());
        _targetMock2.Setup(h => h.HandleMessage(_messageMock.Object)).Callback(() => latch.Signal());
        _targetMock3.Setup(h => h.HandleMessage(_messageMock.Object)).Callback(() => latch.Signal());
        var dispatcher = new BroadcastingDispatcher(_provider.GetService<IApplicationContext>(), new PartialFailingTaskScheduler(false, true, true));
        dispatcher.AddHandler(_targetMock1.Object);
        dispatcher.AddHandler(_targetMock2.Object);
        dispatcher.AddHandler(_targetMock3.Object);
        dispatcher.Dispatch(_messageMock.Object);
        Assert.True(latch.Wait(3000));
        _targetMock1.Verify(h => h.HandleMessage(_messageMock.Object), Times.Never());
        _targetMock2.Verify(h => h.HandleMessage(_messageMock.Object));
        _targetMock3.Verify(h => h.HandleMessage(_messageMock.Object));
    }

    [Fact]
    public void MultipleTargetsPartialFailureMiddle()
    {
        var latch = new CountdownEvent(2);
        _targetMock1.Setup(h => h.HandleMessage(_messageMock.Object)).Callback(() => latch.Signal());
        _targetMock2.Setup(h => h.HandleMessage(_messageMock.Object)).Callback(() => latch.Signal());
        _targetMock3.Setup(h => h.HandleMessage(_messageMock.Object)).Callback(() => latch.Signal());
        var dispatcher = new BroadcastingDispatcher(_provider.GetService<IApplicationContext>(), new PartialFailingTaskScheduler(true, false, true));
        dispatcher.AddHandler(_targetMock1.Object);
        dispatcher.AddHandler(_targetMock2.Object);
        dispatcher.AddHandler(_targetMock3.Object);
        dispatcher.Dispatch(_messageMock.Object);
        Assert.True(latch.Wait(3000));
        _targetMock1.Verify(h => h.HandleMessage(_messageMock.Object));
        _targetMock2.Verify(h => h.HandleMessage(_messageMock.Object), Times.Never());
        _targetMock3.Verify(h => h.HandleMessage(_messageMock.Object));
    }

    [Fact]
    public void MultipleTargetsPartialFailureLast()
    {
        var latch = new CountdownEvent(2);
        _targetMock1.Setup(h => h.HandleMessage(_messageMock.Object)).Callback(() => latch.Signal());
        _targetMock2.Setup(h => h.HandleMessage(_messageMock.Object)).Callback(() => latch.Signal());
        _targetMock3.Setup(h => h.HandleMessage(_messageMock.Object)).Callback(() => latch.Signal());
        var dispatcher = new BroadcastingDispatcher(_provider.GetService<IApplicationContext>(), new PartialFailingTaskScheduler(true, true, false));
        dispatcher.AddHandler(_targetMock1.Object);
        dispatcher.AddHandler(_targetMock2.Object);
        dispatcher.AddHandler(_targetMock3.Object);
        dispatcher.Dispatch(_messageMock.Object);
        Assert.True(latch.Wait(3000));
        _targetMock1.Verify(h => h.HandleMessage(_messageMock.Object));
        _targetMock2.Verify(h => h.HandleMessage(_messageMock.Object));
        _targetMock3.Verify(h => h.HandleMessage(_messageMock.Object), Times.Never());
    }

    [Fact]
    public void MultipleTargetsAllFail()
    {
        var dispatcher = new BroadcastingDispatcher(_provider.GetService<IApplicationContext>(), new PartialFailingTaskScheduler(false, false, false));
        dispatcher.AddHandler(_targetMock1.Object);
        dispatcher.AddHandler(_targetMock2.Object);
        dispatcher.AddHandler(_targetMock3.Object);
        dispatcher.Dispatch(_messageMock.Object);
        _targetMock1.Verify(h => h.HandleMessage(_messageMock.Object), Times.Never());
        _targetMock2.Verify(h => h.HandleMessage(_messageMock.Object), Times.Never());
        _targetMock3.Verify(h => h.HandleMessage(_messageMock.Object), Times.Never());
    }

    [Fact]
    public void NoDuplicateSubscription()
    {
        var dispatcher = new BroadcastingDispatcher(_provider.GetService<IApplicationContext>());
        dispatcher.AddHandler(_targetMock1.Object);
        dispatcher.AddHandler(_targetMock1.Object);
        dispatcher.AddHandler(_targetMock1.Object);
        dispatcher.Dispatch(_messageMock.Object);
        _targetMock1.Verify(h => h.HandleMessage(_messageMock.Object));
    }

    [Fact]
    public void RemoveConsumerBeforeSend()
    {
        var dispatcher = new BroadcastingDispatcher(_provider.GetService<IApplicationContext>());
        dispatcher.AddHandler(_targetMock1.Object);
        dispatcher.AddHandler(_targetMock2.Object);
        dispatcher.AddHandler(_targetMock3.Object);
        dispatcher.RemoveHandler(_targetMock2.Object);
        dispatcher.Dispatch(_messageMock.Object);
        _targetMock1.Verify(h => h.HandleMessage(_messageMock.Object));
        _targetMock2.Verify(h => h.HandleMessage(_messageMock.Object), Times.Never());
        _targetMock3.Verify(h => h.HandleMessage(_messageMock.Object));
    }

    [Fact]
    public void RemoveConsumerBetweenSends()
    {
        var dispatcher = new BroadcastingDispatcher(_provider.GetService<IApplicationContext>());
        dispatcher.AddHandler(_targetMock1.Object);
        dispatcher.AddHandler(_targetMock2.Object);
        dispatcher.AddHandler(_targetMock3.Object);
        dispatcher.Dispatch(_messageMock.Object);
        dispatcher.RemoveHandler(_targetMock2.Object);
        dispatcher.Dispatch(_messageMock.Object);
        _targetMock1.Verify(h => h.HandleMessage(_messageMock.Object), Times.Exactly(2));
        _targetMock2.Verify(h => h.HandleMessage(_messageMock.Object), Times.Exactly(1));
        _targetMock3.Verify(h => h.HandleMessage(_messageMock.Object), Times.Exactly(2));
    }

    [Fact]
    public void ApplySequenceDisabledByDefault()
    {
        var dispatcher = new BroadcastingDispatcher(_provider.GetService<IApplicationContext>());
        var messages = new ConcurrentQueue<IMessage>();
        var target1 = new MessageStoringTestEndpoint(messages);
        var target2 = new MessageStoringTestEndpoint(messages);
        dispatcher.AddHandler(target1);
        dispatcher.AddHandler(target2);
        dispatcher.Dispatch(Message.Create("test"));
        Assert.Equal(2, messages.Count);

        Assert.True(messages.TryDequeue(out IMessage message));
        message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.SequenceNumber, out object value);
        Assert.Null(value);
        message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.SequenceSize, out value);
        Assert.Null(value);

        Assert.True(messages.TryDequeue(out message));
        message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.SequenceNumber, out value);
        Assert.Null(value);
        message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.SequenceSize, out value);
        Assert.Null(value);
    }

    [Fact]
    public void ApplySequenceEnabled()
    {
        var dispatcher = new BroadcastingDispatcher(_provider.GetService<IApplicationContext>())
        {
            ApplySequence = true
        };

        var messages = new ConcurrentQueue<IMessage>();
        var target1 = new MessageStoringTestEndpoint(messages);
        var target2 = new MessageStoringTestEndpoint(messages);
        var target3 = new MessageStoringTestEndpoint(messages);
        dispatcher.AddHandler(target1);
        dispatcher.AddHandler(target2);
        dispatcher.AddHandler(target3);
        IMessage inputMessage = Message.Create("test");
        string originalId = inputMessage.Headers.Id;
        dispatcher.Dispatch(inputMessage);
        Assert.Equal(3, messages.Count);

        Assert.True(messages.TryDequeue(out IMessage message));
        message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.SequenceNumber, out object value);
        Assert.Equal(1, value);
        message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.SequenceSize, out value);
        Assert.Equal(3, value);
        message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.CorrelationId, out value);
        Assert.Equal(originalId, value);

        Assert.True(messages.TryDequeue(out message));
        message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.SequenceNumber, out value);
        Assert.Equal(2, value);
        message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.SequenceSize, out value);
        Assert.Equal(3, value);
        message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.CorrelationId, out value);
        Assert.Equal(originalId, value);

        Assert.True(messages.TryDequeue(out message));
        message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.SequenceNumber, out value);
        Assert.Equal(3, value);
        message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.SequenceSize, out value);
        Assert.Equal(3, value);
        message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.CorrelationId, out value);
        Assert.Equal(originalId, value);
    }

    [Fact]
    public void TestExceptionEnhancement()
    {
        var dispatcher = new BroadcastingDispatcher(_provider.GetService<IApplicationContext>());
        dispatcher.AddHandler(_targetMock1.Object);
        _targetMock1.Setup(h => h.HandleMessage(_messageMock.Object)).Throws(new MessagingException("Mock Exception"));

        try
        {
            dispatcher.Dispatch(_messageMock.Object);
            throw new Exception("Expected Exception");
        }
        catch (MessagingException e)
        {
            Assert.Equal(_messageMock.Object, e.FailedMessage);
        }
    }

    [Fact]
    public void TestNoExceptionEnhancement()
    {
        var dispatcher = new BroadcastingDispatcher(_provider.GetService<IApplicationContext>());
        dispatcher.AddHandler(_targetMock1.Object);
        _targetMock1.Object.HandleMessage(_messageMock.Object);
        IMessage doNotReplaceThisMessage = IntegrationMessageBuilder.WithPayload("x").Build();
        _targetMock1.Setup(h => h.HandleMessage(_messageMock.Object)).Throws(new MessagingException(doNotReplaceThisMessage, "Mock Exception"));

        try
        {
            dispatcher.Dispatch(_messageMock.Object);
            throw new Exception("Expected Exception");
        }
        catch (MessagingException e)
        {
            Assert.Equal(doNotReplaceThisMessage, e.FailedMessage);
        }
    }

    [Fact]
    public void TestNoHandler()
    {
        var dispatcher = new BroadcastingDispatcher(_provider.GetService<IApplicationContext>());
        Assert.True(dispatcher.Dispatch(_messageMock.Object));
    }

    [Fact]
    public void TestNoHandlerWithExecutor()
    {
        var dispatcher = new BroadcastingDispatcher(_provider.GetService<IApplicationContext>(), TaskScheduler.Default);
        Assert.True(dispatcher.Dispatch(_messageMock.Object));
    }

    [Fact]
    public void TestNoHandlerWithRequiredSubscriber()
    {
        var dispatcher = new BroadcastingDispatcher(_provider.GetService<IApplicationContext>(), true);

        try
        {
            dispatcher.Dispatch(_messageMock.Object);
            throw new Exception("Expected Exception");
        }
        catch (MessageDispatchingException exception)
        {
            Assert.Equal(_messageMock.Object, exception.FailedMessage);
        }
    }

    [Fact]
    public void TestNoHandlerWithExecutorWithRequiredSubscriber()
    {
        var dispatcher = new BroadcastingDispatcher(_provider.GetService<IApplicationContext>(), TaskScheduler.Default, true);

        try
        {
            dispatcher.Dispatch(_messageMock.Object);
            throw new Exception("Expected Exception");
        }
        catch (MessageDispatchingException exception)
        {
            Assert.Equal(_messageMock.Object, exception.FailedMessage);
        }
    }

    private sealed class MessageStoringTestEndpoint : IMessageHandler
    {
        public ConcurrentQueue<IMessage> MessageList { get; }

        public string ServiceName { get; set; } = nameof(MessageStoringTestEndpoint);

        public MessageStoringTestEndpoint(ConcurrentQueue<IMessage> messageList)
        {
            MessageList = messageList;
        }

        public void HandleMessage(IMessage message)
        {
            MessageList.Enqueue(message);
        }
    }

    private sealed class PartialFailingTaskScheduler : TaskScheduler
    {
        private readonly bool[] _failures;
        private int _count = -1;

        public PartialFailingTaskScheduler(params bool[] failures)
        {
            _failures = failures;
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return new List<Task>();
        }

        protected override void QueueTask(Task task)
        {
            int val = Interlocked.Increment(ref _count);

            if (val < _failures.Length && _failures[val])
            {
                TryExecuteTask(task);
            }
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return false;
        }
    }
}
