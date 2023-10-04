// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Threading.Channels;
using FluentAssertions;
using Steeltoe.Integration.Channel;
using Steeltoe.Messaging;
using Xunit;

namespace Steeltoe.Integration.Test.Channel;

public sealed class QueueChannelTest
{
    private static readonly TimeSpan DefaultTestTimeout = TimeSpan.FromSeconds(5);
    private static readonly Action<AbstractMessageChannel> SendTestMessage = channel => channel.Send(Message.Create("testing"));

    private static readonly Func<AbstractPollableChannel, int?, IMessage?> ReceiveTestMessage = (channel, timeout) =>
        timeout == null ? channel.Receive() : channel.Receive(timeout.Value);

    [Fact]
    public void TestSimpleSendAndReceive()
    {
        var channel = new QueueChannel();

        AssertMessageExchange(() => ReceiveTestMessage(channel, null) != null, () => SendTestMessage(channel));
    }

    [Fact]
    public void TestSimpleSendAndReceiveWithNonBlockingQueue()
    {
        var boundedChannel = System.Threading.Channels.Channel.CreateBounded<IMessage>(new BoundedChannelOptions(int.MaxValue)
        {
            FullMode = BoundedChannelFullMode.DropWrite
        });

        var channel = new QueueChannel(null, boundedChannel);

        AssertMessageExchange(() => ReceiveTestMessage(channel, null) != null, () => SendTestMessage(channel));
    }

    [Fact]
    public void TestSimpleSendAndReceiveWithNonBlockingQueueWithTimeout()
    {
        var boundedChannel = System.Threading.Channels.Channel.CreateBounded<IMessage>(new BoundedChannelOptions(int.MaxValue)
        {
            FullMode = BoundedChannelFullMode.DropWrite
        });

        var channel = new QueueChannel(null, boundedChannel);
        const int receiveTimeout = 500;

        AssertMessageExchange(() => ReceiveTestMessage(channel, receiveTimeout) != null, () => SendTestMessage(channel));
    }

    [Fact]
    public void TestSimpleSendAndReceiveWithTimeout()
    {
        var channel = new QueueChannel();
        const int receiveTimeout = 500;

        AssertMessageExchange(() => ReceiveTestMessage(channel, receiveTimeout) != null, () => SendTestMessage(channel));
    }

    [Fact]
    public void TestImmediateReceive()
    {
        var channel = new QueueChannel();
        const int receiveTimeout = 0;

        AssertMessageExchange(() => ReceiveTestMessage(channel, receiveTimeout) == null, null);
        SendTestMessage(channel);
        AssertMessageExchange(() => ReceiveTestMessage(channel, receiveTimeout) != null, null);
    }

    [Fact]
    public async Task TestBlockingReceiveAsyncWithNoTimeout()
    {
        var channel = new QueueChannel();
        var cancellationTokenSource = new CancellationTokenSource();

        await AssertMessageExchangeAsync(async () => await channel.ReceiveAsync(cancellationTokenSource.Token) == null, cancellationTokenSource.Cancel);
    }

    [Fact]
    public void TestBlockingReceiveWithTimeout()
    {
        var channel = new QueueChannel();
        const int receiveTimeout = 500;

        AssertMessageExchange(() => ReceiveTestMessage(channel, receiveTimeout) == null, null);
    }

    [Fact]
    public async Task TestBlockingReceiveAsyncWithTimeout()
    {
        var channel = new QueueChannel();
        const int receiveTimeout = 500;

        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(receiveTimeout);

        await AssertMessageExchangeAsync(async () => await channel.ReceiveAsync(cancellationTokenSource.Token) == null, cancellationTokenSource.Cancel);
    }

    [Fact]
    public void TestImmediateSend()
    {
        var channel = new QueueChannel(null, 3);
        const int receiveTimeout = 500;
        const int receiveTimeoutZero = 0;

        channel.Send(Message.Create("test-1")).Should().BeTrue();
        channel.Send(Message.Create("test-2"), receiveTimeout).Should().BeTrue();
        channel.Send(Message.Create("test-3"), receiveTimeoutZero).Should().BeTrue();
        channel.Send(Message.Create("test-4"), receiveTimeoutZero).Should().BeFalse();
    }

    [Fact]
    public async Task TestBlockingSendAsyncWithNoTimeout()
    {
        var channel = new QueueChannel(null, 1);

        (await channel.SendAsync(Message.Create("test-1"))).Should().BeTrue();

        var cancellationTokenSource = new CancellationTokenSource();

        await AssertMessageExchangeAsync(async () => !await channel.SendAsync(Message.Create("test-2"), cancellationTokenSource.Token),
            cancellationTokenSource.Cancel);
    }

    [Fact]
    public void TestBlockingSendWithTimeout()
    {
        var channel = new QueueChannel(null, 1);

        channel.Send(Message.Create("test-1")).Should().BeTrue();

        const int sendTimeout = 500;
        AssertMessageExchange(() => !channel.Send(Message.Create("test-2"), sendTimeout), null);
    }

    [Fact]
    public async Task TestBlockingSendAsyncWithTimeout()
    {
        var channel = new QueueChannel(null, 1);

        (await channel.SendAsync(Message.Create("test-1"))).Should().BeTrue();

        var cancellationTokenSource = new CancellationTokenSource();
        const int sendTimeout = 500;

        await AssertMessageExchangeAsync(async () =>
        {
            cancellationTokenSource.CancelAfter(sendTimeout);
            return !await channel.SendAsync(Message.Create("test-2"), cancellationTokenSource.Token);
        }, null);
    }

    [Fact]
    public void TestClear()
    {
        var channel = new QueueChannel(null, 2);

        IMessage<string> message1 = Message.Create("test1");
        IMessage<string> message2 = Message.Create("test2");
        IMessage<string> message3 = Message.Create("test3");

        channel.Send(message1).Should().BeTrue();
        channel.Send(message2).Should().BeTrue();
        channel.Send(message3, 0).Should().BeFalse();

        channel.QueueSize.Should().Be(2);
        channel.RemainingCapacity.Should().Be(2 - 2);

        IList<IMessage> clearedMessages = channel.Clear();
        clearedMessages.Should().HaveCount(2);

        channel.QueueSize.Should().Be(0);
        channel.RemainingCapacity.Should().Be(2);

        channel.Send(message3).Should().BeTrue();
    }

    [Fact]
    public void TestClearEmptyChannel()
    {
        var channel = new QueueChannel();

        IList<IMessage> clearedMessages = channel.Clear();

        clearedMessages.Should().BeEmpty();
    }

    [Fact]
    public void TestPurge()
    {
        var channel = new QueueChannel();

        Func<IList<IMessage>> action = () => channel.Purge(null);
        action.Should().ThrowExactly<NotSupportedException>();
    }

    private static void AssertMessageExchange(Func<bool> backgroundOperation, Action? foregroundOperation)
    {
        Task backgroundTask = Task.Run(() =>
        {
            bool succeeded = backgroundOperation();
            succeeded.Should().BeTrue("background operation should succeed");
        });

        foregroundOperation?.Invoke();

        bool succeeded = backgroundTask.Wait(DefaultTestTimeout);

        if (!succeeded)
        {
            throw new TimeoutException($"Background operation timed out unexpectedly after {DefaultTestTimeout}.");
        }
    }

    private static async Task AssertMessageExchangeAsync(Func<Task<bool>> backgroundAsyncOperation, Action? foregroundOperation)
    {
        Task backgroundTask = Task.Run(async () =>
        {
            bool succeeded = await backgroundAsyncOperation();
            succeeded.Should().BeTrue("async background operation should succeed");
        });

        foregroundOperation?.Invoke();

        await backgroundTask.WaitAsync(DefaultTestTimeout);
    }
}
