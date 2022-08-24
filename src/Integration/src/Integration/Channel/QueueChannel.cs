// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging;

namespace Steeltoe.Integration.Channel;

public class QueueChannel : AbstractPollableChannel, IQueueChannelOperations
{
    private readonly Channel<IMessage> _channel;
    private readonly int _capacity = -1;
    private int _size;

    public int QueueSize => _size;

    public int RemainingCapacity => _capacity - _size;

    public QueueChannel(ILogger logger = null)
        : this(null, logger)
    {
    }

    public QueueChannel(IApplicationContext context, ILogger logger = null)
        : this(context, System.Threading.Channels.Channel.CreateBounded<IMessage>(new BoundedChannelOptions(int.MaxValue)
        {
            FullMode = BoundedChannelFullMode.Wait
        }), null, logger)
    {
        _capacity = int.MaxValue;
    }

    public QueueChannel(IApplicationContext context, string name, ILogger logger = null)
        : this(context, System.Threading.Channels.Channel.CreateBounded<IMessage>(new BoundedChannelOptions(int.MaxValue)
        {
            FullMode = BoundedChannelFullMode.Wait
        }), name, logger)
    {
        _capacity = int.MaxValue;
    }

    public QueueChannel(IApplicationContext context, int capacity, ILogger logger = null)
        : this(context, System.Threading.Channels.Channel.CreateBounded<IMessage>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        }), null, logger)
    {
        _capacity = capacity;
    }

    public QueueChannel(IApplicationContext context, Channel<IMessage> channel, ILogger logger = null)
        : this(context, channel, null, logger)
    {
    }

    public QueueChannel(IApplicationContext context, Channel<IMessage> channel, string name, ILogger logger = null)
        : base(context, name, logger)
    {
        ArgumentGuard.NotNull(channel);

        _channel = channel;
        Writer = new QueueChannelWriter(this, logger);
        Reader = new QueueChannelReader(this, logger);
    }

    public IList<IMessage> Clear()
    {
        var messages = new List<IMessage>();

        while (_channel.Reader.TryRead(out IMessage message))
        {
            Interlocked.Decrement(ref _size);
            messages.Add(message);
        }

        return messages;
    }

    public IList<IMessage> Purge(IMessageSelector messageSelector)
    {
        throw new NotSupportedException();
    }

    protected override IMessage DoReceiveInternal(CancellationToken cancellationToken)
    {
        if (cancellationToken == default)
        {
            if (_channel.Reader.TryRead(out IMessage message))
            {
                Interlocked.Decrement(ref _size);
            }

            return message;
        }

        try
        {
            IMessage message = _channel.Reader.ReadAsync(cancellationToken).AsTask().GetAwaiter().GetResult();

            if (message != null)
            {
                Interlocked.Decrement(ref _size);
            }

            return message;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    protected override bool DoSendInternal(IMessage message, CancellationToken cancellationToken)
    {
        if (cancellationToken == default)
        {
            if (_channel.Writer.TryWrite(message))
            {
                Interlocked.Increment(ref _size);
                return true;
            }

            return false;
        }

        try
        {
            _channel.Writer.WriteAsync(message, cancellationToken).AsTask().GetAwaiter().GetResult();
            Interlocked.Increment(ref _size);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
