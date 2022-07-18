// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging;
using System;
using System.Collections.Generic;

namespace Steeltoe.Integration.Support;

public class MutableIntegrationMessageBuilder : AbstractMessageBuilder
{
    protected MutableMessage mutableMessage;

    protected IDictionary<string, object> headers;

    protected MutableIntegrationMessageBuilder()
    {
    }

    private MutableIntegrationMessageBuilder(IMessage message)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        mutableMessage = message as MutableMessage ?? new MutableMessage(message.Payload, message.Headers);

        headers = mutableMessage.RawHeaders;
    }

    public override object Payload => mutableMessage.Payload;

    public override IDictionary<string, object> Headers => headers;

    public static MutableIntegrationMessageBuilder WithPayload(object payload) => WithPayload(payload, true);

    public static MutableIntegrationMessageBuilder WithPayload(object payload, bool generateHeaders)
    {
        var message = generateHeaders
            ? new MutableMessage(payload)
            : new MutableMessage(payload, new MutableMessageHeaders(null, MessageHeaders.IdValueNone, -1L));

        return FromMessage(message);
    }

    public static MutableIntegrationMessageBuilder FromMessage(IMessage message)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        return new MutableIntegrationMessageBuilder(message);
    }

    public override IMessageBuilder SetHeader(string headerName, object headerValue)
    {
        if (headerName == null)
        {
            throw new ArgumentNullException(nameof(headerName));
        }

        if (headerValue == null)
        {
            RemoveHeader(headerName);
        }
        else
        {
            headers[headerName] = headerValue;
        }

        return this;
    }

    public override IMessageBuilder SetHeaderIfAbsent(string headerName, object headerValue)
    {
        if (!headers.ContainsKey(headerName))
        {
            headers.Add(headerName, headerValue);
        }

        return this;
    }

    public override IMessageBuilder RemoveHeaders(params string[] headerPatterns)
    {
        var headersToRemove = new List<string>();
        foreach (var pattern in headerPatterns)
        {
            if (!string.IsNullOrEmpty(pattern))
            {
                if (pattern.Contains('*'))
                {
                    headersToRemove.AddRange(GetMatchingHeaderNames(pattern, headers));
                }
                else
                {
                    headersToRemove.Add(pattern);
                }
            }
        }

        foreach (var headerToRemove in headersToRemove)
        {
            RemoveHeader(headerToRemove);
        }

        return this;
    }

    public override IMessageBuilder RemoveHeader(string headerName)
    {
        if (!string.IsNullOrEmpty(headerName))
        {
            headers.Remove(headerName);
        }

        return this;
    }

    public override IMessageBuilder CopyHeaders(IDictionary<string, object> headersToCopy)
    {
        if (headersToCopy != null)
        {
            foreach (var header in headersToCopy)
            {
                headers.Add(header);
            }
        }

        return this;
    }

    public override IMessageBuilder CopyHeadersIfAbsent(IDictionary<string, object> headersToCopy)
    {
        if (headersToCopy != null)
        {
            foreach (var entry in headersToCopy)
            {
                SetHeaderIfAbsent(entry.Key, entry.Value);
            }
        }

        return this;
    }

    protected override List<List<object>> SequenceDetails
    {
        get
        {
            if (headers.TryGetValue(IntegrationMessageHeaderAccessor.SequenceDetails, out var result))
            {
                return (List<List<object>>)result;
            }

            return null;
        }
    }

    protected override object CorrelationId
    {
        get
        {
            if (headers.TryGetValue(IntegrationMessageHeaderAccessor.CorrelationId, out var result))
            {
                return result;
            }

            return null;
        }
    }

    protected override object SequenceNumber
    {
        get
        {
            if (headers.TryGetValue(IntegrationMessageHeaderAccessor.SequenceNumber, out var result))
            {
                return result;
            }

            return null;
        }
    }

    protected override object SequenceSize
    {
        get
        {
            if (headers.TryGetValue(IntegrationMessageHeaderAccessor.SequenceSize, out var result))
            {
                return result;
            }

            return null;
        }
    }

    public override IMessage Build() => mutableMessage;

    protected List<string> GetMatchingHeaderNames(string pattern, IDictionary<string, object> headers)
    {
        var matchingHeaderNames = new List<string>();
        if (headers != null)
        {
            foreach (var header in headers)
            {
                if (PatternMatchUtils.SimpleMatch(pattern, header.Key))
                {
                    matchingHeaderNames.Add(header.Key);
                }
            }
        }

        return matchingHeaderNames;
    }
}

public class MutableIntegrationMessageBuilder<T> : MutableIntegrationMessageBuilder, IMessageBuilder<T>
{
    private MutableIntegrationMessageBuilder(IMessage<T> message)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        mutableMessage = message as MutableMessage<T> ?? new MutableMessage<T>(message.Payload, message.Headers);

        headers = mutableMessage.RawHeaders;
    }

    public new T Payload => (T)mutableMessage.Payload;

    public static MutableIntegrationMessageBuilder<T> WithPayload(T payload) => WithPayload(payload, true);

    public static MutableIntegrationMessageBuilder<T> WithPayload(T payload, bool generateHeaders)
    {
        var message = generateHeaders
            ? new MutableMessage<T>(payload)
            : new MutableMessage<T>(payload, new MutableMessageHeaders(null, MessageHeaders.IdValueNone, -1L));

        return FromMessage(message);
    }

    public static MutableIntegrationMessageBuilder<T> FromMessage(IMessage<T> message)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        return new MutableIntegrationMessageBuilder<T>(message);
    }

    public new IMessageBuilder<T> SetHeader(string headerName, object headerValue)
    {
        base.SetHeader(headerName, headerValue);
        return this;
    }

    public new IMessageBuilder<T> SetHeaderIfAbsent(string headerName, object headerValue)
    {
        base.SetHeaderIfAbsent(headerName, headerValue);
        return this;
    }

    public new IMessageBuilder<T> RemoveHeaders(params string[] headerPatterns)
    {
        base.RemoveHeaders(headerPatterns);
        return this;
    }

    public new IMessageBuilder<T> RemoveHeader(string headerName)
    {
        base.RemoveHeader(headerName);
        return this;
    }

    public new IMessageBuilder<T> CopyHeaders(IDictionary<string, object> headersToCopy)
    {
        base.CopyHeaders(headersToCopy);
        return this;
    }

    public new IMessageBuilder<T> CopyHeadersIfAbsent(IDictionary<string, object> headersToCopy)
    {
        base.CopyHeadersIfAbsent(headersToCopy);
        return this;
    }

    public new IMessage<T> Build()
    {
        return (IMessage<T>)mutableMessage;
    }

    public new IMessageBuilder<T> SetExpirationDate(long expirationDate)
    {
        base.SetExpirationDate(expirationDate);
        return this;
    }

    public new IMessageBuilder<T> SetExpirationDate(DateTime? expirationDate)
    {
        base.SetExpirationDate(expirationDate);
        return this;
    }

    public new IMessageBuilder<T> SetCorrelationId(object correlationId)
    {
        base.SetCorrelationId(correlationId);
        return this;
    }

    public new IMessageBuilder<T> PushSequenceDetails(object correlationId, int sequenceNumber, int sequenceSize)
    {
        base.PushSequenceDetails(correlationId, sequenceNumber, sequenceSize);
        return this;
    }

    public new IMessageBuilder<T> PopSequenceDetails()
    {
        base.PopSequenceDetails();
        return this;
    }

    public new IMessageBuilder<T> SetReplyChannel(IMessageChannel replyChannel)
    {
        base.SetReplyChannel(replyChannel);
        return this;
    }

    public new IMessageBuilder<T> SetReplyChannelName(string replyChannelName)
    {
        base.SetReplyChannelName(replyChannelName);
        return this;
    }

    public new IMessageBuilder<T> SetErrorChannel(IMessageChannel errorChannel)
    {
        base.SetErrorChannel(errorChannel);
        return this;
    }

    public new IMessageBuilder<T> SetErrorChannelName(string errorChannelName)
    {
        base.SetErrorChannelName(errorChannelName);
        return this;
    }

    public new IMessageBuilder<T> SetSequenceNumber(int sequenceNumber)
    {
        base.SetSequenceNumber(sequenceNumber);
        return this;
    }

    public new IMessageBuilder<T> SetSequenceSize(int sequenceSize)
    {
        base.SetSequenceSize(sequenceSize);
        return this;
    }

    public new IMessageBuilder<T> SetPriority(int priority)
    {
        base.SetPriority(priority);
        return this;
    }

    public new IMessageBuilder<T> FilterAndCopyHeadersIfAbsent(IDictionary<string, object> headersToCopy, params string[] headerPatternsToFilter)
    {
        base.FilterAndCopyHeadersIfAbsent(headersToCopy, headerPatternsToFilter);
        return this;
    }
}
