// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging;

namespace Steeltoe.Integration.Support;

public abstract class AbstractMessageBuilder : IMessageBuilder
{
    protected readonly object InnerPayload;

    protected readonly IMessage OriginalMessage;

    protected readonly IntegrationMessageHeaderAccessor HeaderAccessor;

    protected volatile bool modified;

    protected IList<string> readOnlyHeaders;

    protected AbstractMessageBuilder()
    {
    }

    protected AbstractMessageBuilder(object payload, IMessage originalMessage)
    {
        this.InnerPayload = payload ?? throw new ArgumentNullException(nameof(payload));
        this.OriginalMessage = originalMessage;
        HeaderAccessor = new IntegrationMessageHeaderAccessor(originalMessage);
        if (originalMessage != null)
        {
            modified = !this.InnerPayload.Equals(originalMessage.Payload);
        }
    }

    public virtual IMessageBuilder SetExpirationDate(long expirationDate)
    {
        return SetHeader(IntegrationMessageHeaderAccessor.ExpirationDate, expirationDate);
    }

    public virtual IMessageBuilder SetExpirationDate(DateTime? expirationDate)
    {
        if (expirationDate != null)
        {
            var datetime = new DateTimeOffset(expirationDate.Value);
            return SetHeader(IntegrationMessageHeaderAccessor.ExpirationDate, datetime.ToUnixTimeMilliseconds());
        }
        else
        {
            return SetHeader(IntegrationMessageHeaderAccessor.ExpirationDate, null);
        }
    }

    public virtual IMessageBuilder SetCorrelationId(object correlationId)
    {
        return SetHeader(IntegrationMessageHeaderAccessor.CorrelationId, correlationId);
    }

    public virtual IMessageBuilder PushSequenceDetails(object correlationId, int sequenceNumber, int sequenceSize)
    {
        var incomingCorrelationId = CorrelationId;
        var incomingSequenceDetails = SequenceDetails;
        if (incomingCorrelationId != null)
        {
            incomingSequenceDetails = incomingSequenceDetails == null ? new List<List<object>>() : new List<List<object>>(incomingSequenceDetails);
            incomingSequenceDetails.Add(new List<object> { incomingCorrelationId, SequenceNumber, SequenceSize });
        }

        if (incomingSequenceDetails != null)
        {
            SetHeader(IntegrationMessageHeaderAccessor.SequenceDetails, incomingSequenceDetails);
        }

        return SetCorrelationId(correlationId)
            .SetSequenceNumber(sequenceNumber)
            .SetSequenceSize(sequenceSize);
    }

    public virtual IMessageBuilder PopSequenceDetails()
    {
        var incomingSequenceDetails = SequenceDetails;
        if (incomingSequenceDetails == null)
        {
            return this;
        }
        else
        {
            incomingSequenceDetails = new List<List<object>>(incomingSequenceDetails);
        }

        var sequenceDetails = incomingSequenceDetails[incomingSequenceDetails.Count - 1];
        incomingSequenceDetails.RemoveAt(incomingSequenceDetails.Count - 1);
        if (sequenceDetails.Count != 3)
        {
            throw new InvalidOperationException("Wrong sequence details (not created by MessageBuilder?)");
        }

        SetCorrelationId(sequenceDetails[0]);
        var sequenceNumber = sequenceDetails[1] as int?;
        var sequenceSize = sequenceDetails[2] as int?;
        if (sequenceNumber.HasValue)
        {
            SetSequenceNumber(sequenceNumber.Value);
        }

        if (sequenceSize.HasValue)
        {
            SetSequenceSize(sequenceSize.Value);
        }

        if (incomingSequenceDetails.Count > 0)
        {
            SetHeader(IntegrationMessageHeaderAccessor.SequenceDetails, incomingSequenceDetails);
        }
        else
        {
            RemoveHeader(IntegrationMessageHeaderAccessor.SequenceDetails);
        }

        return this;
    }

    public virtual IMessageBuilder SetReplyChannel(IMessageChannel replyChannel)
    {
        return SetHeader(MessageHeaders.ReplyChannelName, replyChannel);
    }

    public virtual IMessageBuilder SetReplyChannelName(string replyChannelName)
    {
        return SetHeader(MessageHeaders.ReplyChannelName, replyChannelName);
    }

    public virtual IMessageBuilder SetErrorChannel(IMessageChannel errorChannel)
    {
        return SetHeader(MessageHeaders.ErrorChannelName, errorChannel);
    }

    public virtual IMessageBuilder SetErrorChannelName(string errorChannelName)
    {
        return SetHeader(MessageHeaders.ErrorChannelName, errorChannelName);
    }

    public virtual IMessageBuilder SetSequenceNumber(int sequenceNumber)
    {
        return SetHeader(IntegrationMessageHeaderAccessor.SequenceNumber, sequenceNumber);
    }

    public virtual IMessageBuilder SetSequenceSize(int sequenceSize)
    {
        return SetHeader(IntegrationMessageHeaderAccessor.SequenceSize, sequenceSize);
    }

    public virtual IMessageBuilder SetPriority(int priority)
    {
        return SetHeader(IntegrationMessageHeaderAccessor.Priority, priority);
    }

    public virtual IMessageBuilder FilterAndCopyHeadersIfAbsent(IDictionary<string, object> headersToCopy, params string[] headerPatternsToFilter)
    {
        IDictionary<string, object> headers = new Dictionary<string, object>(headersToCopy);

        if (headerPatternsToFilter?.Length > 0)
        {
            foreach (var entry in headersToCopy)
            {
                if (PatternMatchUtils.SimpleMatch(headerPatternsToFilter, entry.Key))
                {
                    headers.Remove(entry.Key);
                }
            }
        }

        return CopyHeadersIfAbsent(headers);
    }

    public abstract object Payload { get; }

    public abstract IDictionary<string, object> Headers { get; }

    public abstract IMessageBuilder SetHeader(string headerName, object headerValue);

    public abstract IMessageBuilder SetHeaderIfAbsent(string headerName, object headerValue);

    public abstract IMessageBuilder RemoveHeaders(params string[] headerPatterns);

    public abstract IMessageBuilder RemoveHeader(string headerName);

    public abstract IMessageBuilder CopyHeaders(IDictionary<string, object> headersToCopy);

    public abstract IMessageBuilder CopyHeadersIfAbsent(IDictionary<string, object> headersToCopy);

    public abstract IMessage Build();

    protected abstract List<List<object>> SequenceDetails { get; }

    protected abstract object CorrelationId { get; }

    protected abstract object SequenceNumber { get; }

    protected abstract object SequenceSize { get; }

    protected bool ContainsReadOnly(IMessageHeaders headers)
    {
        if (readOnlyHeaders != null)
        {
            foreach (var readOnly in readOnlyHeaders)
            {
                if (headers.ContainsKey(readOnly))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
