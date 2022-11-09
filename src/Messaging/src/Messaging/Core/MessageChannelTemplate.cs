// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging.Support;

namespace Steeltoe.Messaging.Core;

public class MessageChannelTemplate : AbstractDestinationResolvingMessagingTemplate<IMessageChannel>
{
    public const string DefaultSendTimeoutHeader = "sendTimeout";

    public const string DefaultReceiveTimeoutHeader = "receiveTimeout";

    private readonly ILogger _logger;

    private volatile int _sendTimeout = -1;

    private volatile int _receiveTimeout = -1;

    private string _sendTimeoutHeader = DefaultSendTimeoutHeader;

    private string _receiveTimeoutHeader = DefaultReceiveTimeoutHeader;

    private volatile bool _throwExceptionOnLateReply;

    public virtual int SendTimeout
    {
        get => _sendTimeout;
        set => _sendTimeout = value;
    }

    public virtual int ReceiveTimeout
    {
        get => _receiveTimeout;
        set => _receiveTimeout = value;
    }

    public virtual string SendTimeoutHeader
    {
        get => _sendTimeoutHeader;
        set
        {
            ArgumentGuard.NotNull(value);

            _sendTimeoutHeader = value;
        }
    }

    public virtual string ReceiveTimeoutHeader
    {
        get => _receiveTimeoutHeader;
        set
        {
            ArgumentGuard.NotNull(value);

            _receiveTimeoutHeader = value;
        }
    }

    public virtual bool ThrowExceptionOnLateReply
    {
        get => _throwExceptionOnLateReply;
        set => _throwExceptionOnLateReply = value;
    }

    public MessageChannelTemplate(ILogger logger = null)
        : base(null)
    {
        _logger = logger;
    }

    public MessageChannelTemplate(IApplicationContext context, ILogger logger = null)
        : base(context)
    {
        _logger = logger;
    }

    protected virtual IMessage ProcessMessageBeforeSend(IMessage message)
    {
        IMessage messageToSend = message;
        MessageHeaderAccessor accessor = MessageHeaderAccessor.GetAccessor(message, typeof(MessageHeaderAccessor));

        if (accessor != null && accessor.IsMutable)
        {
            accessor.RemoveHeader(_sendTimeoutHeader);
            accessor.RemoveHeader(_receiveTimeoutHeader);
            accessor.SetImmutable();
        }
        else if (message.Headers.ContainsKey(_sendTimeoutHeader) || message.Headers.ContainsKey(_receiveTimeoutHeader))
        {
            messageToSend = MessageBuilder.FromMessage(message).SetHeader(_sendTimeoutHeader, null).SetHeader(_receiveTimeoutHeader, null).Build();
        }

        return messageToSend;
    }

    protected override void DoSend(IMessageChannel destination, IMessage message)
    {
        DoSend(destination, message, GetSendTimeout(message));
    }

    protected void DoSend(IMessageChannel channel, IMessage message, int timeout)
    {
        ArgumentGuard.NotNull(channel);

        IMessage messageToSend = ProcessMessageBeforeSend(message);
        bool sent = channel.Send(messageToSend, timeout);

        if (!sent)
        {
            throw new MessageDeliveryException(message, $"Failed to send message to channel '{channel}");
        }
    }

    protected override Task DoSendAsync(IMessageChannel destination, IMessage message, CancellationToken cancellationToken)
    {
        return DoSendAsync(destination, message, GetSendTimeout(message), cancellationToken);
    }

    protected Task DoSendAsync(IMessageChannel channel, IMessage message, int timeout, CancellationToken cancellationToken = default)
    {
        ArgumentGuard.NotNull(channel);

        return DoSendInternalAsync(channel, message, timeout, cancellationToken);
    }

    protected async Task DoSendInternalAsync(IMessageChannel channel, IMessage message, int timeout, CancellationToken cancellationToken = default)
    {
        IMessage messageToSend = ProcessMessageBeforeSend(message);
        bool sent = false;

        if (cancellationToken == default)
        {
            using var source = new CancellationTokenSource();
            source.CancelAfter(timeout);
            sent = await channel.SendAsync(messageToSend, source.Token);
        }
        else
        {
            sent = await channel.SendAsync(messageToSend, cancellationToken);
        }

        if (!sent)
        {
            throw new MessageDeliveryException(message, $"Failed to send message to channel '{channel}");
        }
    }

    protected override IMessage DoReceive(IMessageChannel destination)
    {
        return DoReceive(destination, ReceiveTimeout);
    }

    protected IMessage DoReceive(IMessageChannel channel, int timeout)
    {
        ArgumentGuard.NotNull(channel);

        if (channel is not IPollableChannel pollableChannel)
        {
            throw new InvalidOperationException("A PollableChannel is required to receive messages");
        }

        IMessage message = pollableChannel.Receive(timeout);

        if (message == null)
        {
            _logger?.LogTrace("Failed to receive message from channel '{channel}'", channel);
        }

        return message;
    }

    protected override Task<IMessage> DoReceiveAsync(IMessageChannel destination, CancellationToken cancellationToken)
    {
        return DoReceiveAsync(destination, ReceiveTimeout, cancellationToken);
    }

    protected Task<IMessage> DoReceiveAsync(IMessageChannel channel, int timeout, CancellationToken cancellationToken = default)
    {
        ArgumentGuard.NotNull(channel);

        if (channel is not IPollableChannel)
        {
            throw new InvalidOperationException("A PollableChannel is required to receive messages");
        }

        return DoReceiveInternalAsync(channel, timeout, cancellationToken);
    }

    protected async Task<IMessage> DoReceiveInternalAsync(IMessageChannel channel, int timeout, CancellationToken cancellationToken = default)
    {
        IMessage message = null;

        if (cancellationToken == default)
        {
            using var source = new CancellationTokenSource();
            source.CancelAfter(timeout);
            message = await ((IPollableChannel)channel).ReceiveAsync(source.Token);
        }
        else
        {
            message = await ((IPollableChannel)channel).ReceiveAsync(cancellationToken);
        }

        if (message == null)
        {
            _logger?.LogTrace("Failed to receive message from channel '{channel}'", channel);
        }

        return message;
    }

    protected override Task<IMessage> DoSendAndReceiveAsync(IMessageChannel destination, IMessage requestMessage, CancellationToken cancellationToken = default)
    {
        ArgumentGuard.NotNull(destination);

        return DoSendAndReceiveInternalAsync(destination, requestMessage, cancellationToken);
    }

    protected async Task<IMessage> DoSendAndReceiveInternalAsync(IMessageChannel channel, IMessage requestMessage,
        CancellationToken cancellationToken = default)
    {
        object originalReplyChannelHeader = requestMessage.Headers.ReplyChannel;
        object originalErrorChannelHeader = requestMessage.Headers.ErrorChannel;

        int sendTimeout = GetSendTimeout(requestMessage);
        int receiveTimeout = GetReceiveTimeout(requestMessage);

        var tempReplyChannel = new TemporaryReplyChannel(_throwExceptionOnLateReply);

        requestMessage = MessageBuilder.FromMessage(requestMessage).SetReplyChannel(tempReplyChannel).SetHeader(_sendTimeoutHeader, null)
            .SetHeader(_receiveTimeoutHeader, null).SetErrorChannel(tempReplyChannel).Build();

        try
        {
            await DoSendAsync(channel, requestMessage, sendTimeout, cancellationToken);
        }
        catch (Exception)
        {
            tempReplyChannel.SendFailed = true;
            throw;
        }

        IMessage replyMessage = await DoReceiveAsync(tempReplyChannel, receiveTimeout, cancellationToken);

        if (replyMessage != null)
        {
            replyMessage = MessageBuilder.FromMessage(replyMessage).SetHeader(MessageHeaders.ReplyChannelName, originalReplyChannelHeader)
                .SetHeader(MessageHeaders.ErrorChannelName, originalErrorChannelHeader).Build();
        }

        return replyMessage;
    }

    protected override IMessage DoSendAndReceive(IMessageChannel destination, IMessage requestMessage)
    {
        ArgumentGuard.NotNull(destination);

        object originalReplyChannelHeader = requestMessage.Headers.ReplyChannel;
        object originalErrorChannelHeader = requestMessage.Headers.ErrorChannel;

        int sendTimeout = GetSendTimeout(requestMessage);
        int receiveTimeout = GetReceiveTimeout(requestMessage);

        var tempReplyChannel = new TemporaryReplyChannel(_throwExceptionOnLateReply);

        requestMessage = MessageBuilder.FromMessage(requestMessage).SetReplyChannel(tempReplyChannel).SetHeader(_sendTimeoutHeader, null)
            .SetHeader(_receiveTimeoutHeader, null).SetErrorChannel(tempReplyChannel).Build();

        try
        {
            DoSend(destination, requestMessage, sendTimeout);
        }
        catch (Exception)
        {
            tempReplyChannel.SendFailed = true;
            throw;
        }

        IMessage replyMessage = DoReceive(tempReplyChannel, receiveTimeout);

        if (replyMessage != null)
        {
            replyMessage = MessageBuilder.FromMessage(replyMessage).SetHeader(MessageHeaders.ReplyChannelName, originalReplyChannelHeader)
                .SetHeader(MessageHeaders.ErrorChannelName, originalErrorChannelHeader).Build();
        }

        return replyMessage;
    }

    private static int? HeaderToInt(object headerValue)
    {
        if (headerValue is int intValue)
        {
            return intValue;
        }

        if (headerValue is string stringValue)
        {
            return int.Parse(stringValue, CultureInfo.InvariantCulture);
        }

        return null;
    }

    private int GetSendTimeout(IMessage requestMessage)
    {
        if (requestMessage.Headers.TryGetValue(_sendTimeoutHeader, out object headerValue))
        {
            int? result = HeaderToInt(headerValue);

            if (result.HasValue)
            {
                return result.Value;
            }
        }

        return _sendTimeout;
    }

    private int GetReceiveTimeout(IMessage requestMessage)
    {
        if (requestMessage.Headers.TryGetValue(_receiveTimeoutHeader, out object headerValue))
        {
            int? result = HeaderToInt(headerValue);

            if (result.HasValue)
            {
                return result.Value;
            }
        }

        return _receiveTimeout;
    }

    private sealed class TemporaryReplyChannel : IPollableChannel
    {
        private readonly CountdownEvent _replyLatch = new(1);

        private readonly bool _throwExceptionOnLateReply;

        private volatile IMessage _replyMessage;

        public bool SendFailed { get; set; }

        public bool TimedOut { get; set; }

        public bool HasReceived { get; set; }

        public string ServiceName { get; set; } = "TempReplyChannel";

        public TemporaryReplyChannel(bool throwExceptionOnLateReply)
        {
            _throwExceptionOnLateReply = throwExceptionOnLateReply;
        }

        public IMessage Receive()
        {
            return Receive(-1);
        }

        public IMessage Receive(int timeout)
        {
            try
            {
                if (timeout < 0)
                {
                    _replyLatch.Wait();
                    HasReceived = true;
                }
                else
                {
                    if (_replyLatch.Wait(timeout))
                    {
                        HasReceived = true;
                    }
                    else
                    {
                        TimedOut = true;
                    }
                }
            }
            catch (Exception)
            {
                // Log
            }

            return !TimedOut ? _replyMessage : null;
        }

        public ValueTask<IMessage> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _replyLatch.Wait(cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    TimedOut = true;
                }
                else
                {
                    HasReceived = true;
                }
            }
            catch (Exception)
            {
                TimedOut = true;
            }

            return TimedOut ? new ValueTask<IMessage>((IMessage)null) : new ValueTask<IMessage>(_replyMessage);
        }

        public bool Send(IMessage message)
        {
            return Send(message, -1);
        }

        public bool Send(IMessage message, int timeout)
        {
            bool alreadyReceivedReply = HasReceived;

            string errorDescription = null;

            if (TimedOut)
            {
                errorDescription = "Reply message received but the receiving thread has exited due to a timeout";
            }
            else if (alreadyReceivedReply)
            {
                errorDescription = "Reply message received but the receiving thread has already received a reply";
            }
            else if (SendFailed)
            {
                errorDescription = "Reply message received but the receiving thread has exited due to " + "an exception while sending the request message";
            }
            else
            {
                _replyMessage = message;
            }

            _replyLatch.Signal();

            if (errorDescription != null && _throwExceptionOnLateReply)
            {
                throw new MessageDeliveryException(message, errorDescription);
            }

            return true;
        }

        public ValueTask<bool> SendAsync(IMessage message, CancellationToken cancellationToken = default)
        {
            return new ValueTask<bool>(Send(message, -1));
        }
    }
}
