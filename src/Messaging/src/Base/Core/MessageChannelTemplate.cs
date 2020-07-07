// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging.Support;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Core
{
    public class MessageChannelTemplate : AbstractDestinationResolvingMessagingTemplate<IMessageChannel>
    {
        public const string DEFAULT_SEND_TIMEOUT_HEADER = "sendTimeout";

        public const string DEFAULT_RECEIVE_TIMEOUT_HEADER = "receiveTimeout";

        private readonly ILogger _logger;

        private volatile int _sendTimeout = -1;

        private volatile int _receiveTimeout = -1;

        private string _sendTimeoutHeader = DEFAULT_SEND_TIMEOUT_HEADER;

        private string _receiveTimeoutHeader = DEFAULT_RECEIVE_TIMEOUT_HEADER;

        private volatile bool _throwExceptionOnLateReply = false;

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

        public virtual int SendTimeout
        {
            get
            {
                return _sendTimeout;
            }

            set
            {
                _sendTimeout = value;
            }
        }

        public virtual int ReceiveTimeout
        {
            get
            {
                return _receiveTimeout;
            }

            set
            {
                _receiveTimeout = value;
            }
        }

        public virtual string SendTimeoutHeader
        {
            get
            {
                return _sendTimeoutHeader;
            }

            set
            {
                _sendTimeoutHeader = value ?? throw new ArgumentNullException("'sendTimeoutHeader' cannot be null");
            }
        }

        public virtual string ReceiveTimeoutHeader
        {
            get
            {
                return _receiveTimeoutHeader;
            }

            set
            {
                _receiveTimeoutHeader = value ?? throw new ArgumentNullException("'receiveTimeoutHeader' cannot be null");
            }
        }

        public virtual bool ThrowExceptionOnLateReply
        {
            get
            {
                return _throwExceptionOnLateReply;
            }

            set
            {
                _throwExceptionOnLateReply = value;
            }
        }

        protected virtual IMessage ProcessMessageBeforeSend(IMessage message)
        {
            var messageToSend = message;
            var accessor = MessageHeaderAccessor.GetAccessor(message, typeof(MessageHeaderAccessor));
            if (accessor != null && accessor.IsMutable)
            {
                accessor.RemoveHeader(_sendTimeoutHeader);
                accessor.RemoveHeader(_receiveTimeoutHeader);
                accessor.SetImmutable();
            }
            else if (message.Headers.ContainsKey(_sendTimeoutHeader)
                    || message.Headers.ContainsKey(_receiveTimeoutHeader))
            {
                messageToSend = MessageBuilder.FromMessage(message)
                        .SetHeader(_sendTimeoutHeader, null)
                        .SetHeader(_receiveTimeoutHeader, null)
                        .Build();
            }

            return messageToSend;
        }

        protected override void DoSend(IMessageChannel channel, IMessage message)
        {
            DoSend(channel, message, GetSendTimeout(message));
        }

        protected void DoSend(IMessageChannel channel, IMessage message, int timeout)
        {
            if (channel == null)
            {
                throw new ArgumentNullException(nameof(channel));
            }

            var messageToSend = ProcessMessageBeforeSend(message);
            var sent = channel.Send(messageToSend, timeout);

            if (!sent)
            {
                throw new MessageDeliveryException(message, "Failed to send message to channel '" + channel);
            }
        }

        protected override Task DoSendAsync(IMessageChannel channel, IMessage message, CancellationToken cancellationToken)
        {
            return DoSendAsync(channel, message, GetSendTimeout(message), cancellationToken);
        }

        protected Task DoSendAsync(IMessageChannel channel, IMessage message, int timeout, CancellationToken cancellationToken = default)
        {
            if (channel == null)
            {
                throw new ArgumentNullException(nameof(channel));
            }

            return DoSendInternalAsync(channel, message, timeout, cancellationToken);
        }

        protected async Task DoSendInternalAsync(IMessageChannel channel, IMessage message, int timeout, CancellationToken cancellationToken = default)
        {
            var messageToSend = ProcessMessageBeforeSend(message);
            var sent = false;

            if (cancellationToken == default)
            {
                using (var source = new CancellationTokenSource())
                {
                    source.CancelAfter(timeout);
                    sent = await channel.SendAsync(messageToSend, source.Token);
                }
            }
            else
            {
                sent = await channel.SendAsync(messageToSend, cancellationToken);
            }

            if (!sent)
            {
                throw new MessageDeliveryException(message, "Failed to send message to channel '" + channel);
            }
        }

        protected override IMessage DoReceive(IMessageChannel channel)
        {
            return DoReceive(channel, ReceiveTimeout);
        }

        protected IMessage DoReceive(IMessageChannel channel, int timeout)
        {
            if (channel == null)
            {
                throw new ArgumentNullException(nameof(channel));
            }

            if (!(channel is IPollableChannel))
            {
                throw new InvalidOperationException("A PollableChannel is required to receive messages");
            }

            var message = ((IPollableChannel)channel).Receive(timeout);

            if (message == null)
            {
                _logger?.LogTrace("Failed to receive message from channel '{channel}'", channel);
            }

            return message;
        }

        protected override Task<IMessage> DoReceiveAsync(IMessageChannel channel, CancellationToken cancellationToken)
        {
            return DoReceiveAsync(channel, ReceiveTimeout, cancellationToken);
        }

        protected Task<IMessage> DoReceiveAsync(IMessageChannel channel, int timeout, CancellationToken cancellationToken = default)
        {
            if (channel == null)
            {
                throw new ArgumentNullException(nameof(channel));
            }

            if (!(channel is IPollableChannel))
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
                using (var source = new CancellationTokenSource())
                {
                    source.CancelAfter(timeout);
                    message = await ((IPollableChannel)channel).ReceiveAsync(source.Token);
                }
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

        protected override Task<IMessage> DoSendAndReceiveAsync(IMessageChannel channel, IMessage requestMessage, CancellationToken cancellationToken = default)
        {
            if (channel == null)
            {
                throw new ArgumentNullException(nameof(channel));
            }

            return DoSendAndReceiveInternalAsync(channel, requestMessage, cancellationToken);
        }

        protected async Task<IMessage> DoSendAndReceiveInternalAsync(IMessageChannel channel, IMessage requestMessage, CancellationToken cancellationToken = default)
        {
            var originalReplyChannelHeader = requestMessage.Headers.ReplyChannel;
            var originalErrorChannelHeader = requestMessage.Headers.ErrorChannel;

            var sendTimeout = GetSendTimeout(requestMessage);
            var receiveTimeout = GetReceiveTimeout(requestMessage);

            var tempReplyChannel = new TemporaryReplyChannel(_throwExceptionOnLateReply);
            requestMessage = MessageBuilder.FromMessage(requestMessage).SetReplyChannel(tempReplyChannel)
                    .SetHeader(_sendTimeoutHeader, null)
                    .SetHeader(_receiveTimeoutHeader, null)
                    .SetErrorChannel(tempReplyChannel).Build();

            try
            {
                await DoSendAsync(channel, requestMessage, sendTimeout, cancellationToken);
            }
            catch (Exception)
            {
                tempReplyChannel.SendFailed = true;
                throw;
            }

            var replyMessage = await DoReceiveAsync(tempReplyChannel, receiveTimeout, cancellationToken);
            if (replyMessage != null)
            {
                replyMessage = MessageBuilder.FromMessage(replyMessage)
                        .SetHeader(MessageHeaders.REPLY_CHANNEL, originalReplyChannelHeader)
                        .SetHeader(MessageHeaders.ERROR_CHANNEL, originalErrorChannelHeader)
                        .Build();
            }

            return replyMessage;
        }

        protected override IMessage DoSendAndReceive(IMessageChannel channel, IMessage requestMessage)
        {
            if (channel == null)
            {
                throw new ArgumentNullException(nameof(channel));
            }

            var originalReplyChannelHeader = requestMessage.Headers.ReplyChannel;
            var originalErrorChannelHeader = requestMessage.Headers.ErrorChannel;

            var sendTimeout = GetSendTimeout(requestMessage);
            var receiveTimeout = GetReceiveTimeout(requestMessage);

            var tempReplyChannel = new TemporaryReplyChannel(_throwExceptionOnLateReply);
            requestMessage = MessageBuilder.FromMessage(requestMessage).SetReplyChannel(tempReplyChannel)
                    .SetHeader(_sendTimeoutHeader, null)
                    .SetHeader(_receiveTimeoutHeader, null)
                    .SetErrorChannel(tempReplyChannel).Build();

            try
            {
                DoSend(channel, requestMessage, sendTimeout);
            }
            catch (Exception)
            {
                tempReplyChannel.SendFailed = true;
                throw;
            }

            var replyMessage = DoReceive(tempReplyChannel, receiveTimeout);
            if (replyMessage != null)
            {
                replyMessage = MessageBuilder.FromMessage(replyMessage)
                        .SetHeader(MessageHeaders.REPLY_CHANNEL, originalReplyChannelHeader)
                        .SetHeader(MessageHeaders.ERROR_CHANNEL, originalErrorChannelHeader)
                        .Build();
            }

            return replyMessage;
        }

        private int GetSendTimeout(IMessage requestMessage)
        {
            if (requestMessage.Headers.TryGetValue(_sendTimeoutHeader, out var headerValue))
            {
                var result = HeaderToInt(headerValue);
                if (result.HasValue)
                {
                    return result.Value;
                }
            }

            return _sendTimeout;
        }

        private int GetReceiveTimeout(IMessage requestMessage)
        {
            if (requestMessage.Headers.TryGetValue(_receiveTimeoutHeader, out var headerValue))
            {
                var result = HeaderToInt(headerValue);
                if (result.HasValue)
                {
                    return result.Value;
                }
            }

            return _receiveTimeout;
        }

        private int? HeaderToInt(object headerValue)
        {
            if (headerValue is int)
            {
                return (int)headerValue;
            }
            else if (headerValue is string)
            {
                return int.Parse((string)headerValue);
            }
            else
            {
                return null;
            }
        }

        private class TemporaryReplyChannel : IPollableChannel
        {
            private readonly CountdownEvent replyLatch = new CountdownEvent(1);

            private readonly bool throwExceptionOnLateReply;

            private volatile IMessage replyMessage;

            public TemporaryReplyChannel(bool throwExceptionOnLateReply)
            {
                this.throwExceptionOnLateReply = throwExceptionOnLateReply;
            }

            public bool SendFailed { get; set; }

            public bool TimedOut { get; set; }

            public bool HasReceived { get; set; }

            public string ServiceName { get; set; } = "TempReplyChannel";

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
                        replyLatch.Wait();
                        HasReceived = true;
                    }
                    else
                    {
                        if (replyLatch.Wait(timeout))
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

                return !TimedOut ? replyMessage : null;
            }

            public ValueTask<IMessage> ReceiveAsync(CancellationToken cancellationToken = default)
            {
                try
                {
                    replyLatch.Wait(cancellationToken);
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

                return TimedOut ? new ValueTask<IMessage>((IMessage)null) : new ValueTask<IMessage>(replyMessage);
            }

            public bool Send(IMessage message)
            {
                return Send(message, -1);
            }

            public bool Send(IMessage message, int timeout)
            {
                var alreadyReceivedReply = HasReceived;

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
                    errorDescription = "Reply message received but the receiving thread has exited due to " +
                            "an exception while sending the request message";
                }
                else
                {
                    replyMessage = message;
                }

                replyLatch.Signal();

                if (errorDescription != null && throwExceptionOnLateReply)
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
}
