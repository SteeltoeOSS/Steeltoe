// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging;
using System;
using System.Collections.Generic;

namespace Steeltoe.Integration.Support
{
    public abstract class AbstractMessageBuilder : IMessageBuilder
    {
        protected readonly object _payload;

        protected readonly IMessage _originalMessage;

        protected readonly IntegrationMessageHeaderAccessor _headerAccessor;

        protected volatile bool _modified;

        protected IList<string> _readOnlyHeaders;

        protected AbstractMessageBuilder()
        {
        }

        protected AbstractMessageBuilder(object payload, IMessage originalMessage)
        {
            _payload = payload ?? throw new ArgumentNullException(nameof(payload));
            _originalMessage = originalMessage;
            _headerAccessor = new IntegrationMessageHeaderAccessor(originalMessage);
            if (originalMessage != null)
            {
                _modified = !_payload.Equals(originalMessage.Payload);
            }
        }

        public virtual IMessageBuilder SetExpirationDate(long expirationDate)
        {
            return SetHeader(IntegrationMessageHeaderAccessor.EXPIRATION_DATE, expirationDate);
        }

        public virtual IMessageBuilder SetExpirationDate(DateTime? expirationDate)
        {
            if (expirationDate != null)
            {
                var datetime = new DateTimeOffset(expirationDate.Value);
                return SetHeader(IntegrationMessageHeaderAccessor.EXPIRATION_DATE, datetime.ToUnixTimeMilliseconds());
            }
            else
            {
                return SetHeader(IntegrationMessageHeaderAccessor.EXPIRATION_DATE, null);
            }
        }

        public virtual IMessageBuilder SetCorrelationId(object correlationId)
        {
            return SetHeader(IntegrationMessageHeaderAccessor.CORRELATION_ID, correlationId);
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
                SetHeader(IntegrationMessageHeaderAccessor.SEQUENCE_DETAILS, incomingSequenceDetails);
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
                SetHeader(IntegrationMessageHeaderAccessor.SEQUENCE_DETAILS, incomingSequenceDetails);
            }
            else
            {
                RemoveHeader(IntegrationMessageHeaderAccessor.SEQUENCE_DETAILS);
            }

            return this;
        }

        public virtual IMessageBuilder SetReplyChannel(IMessageChannel replyChannel)
        {
            return SetHeader(MessageHeaders.REPLY_CHANNEL, replyChannel);
        }

        public virtual IMessageBuilder SetReplyChannelName(string replyChannelName)
        {
            return SetHeader(MessageHeaders.REPLY_CHANNEL, replyChannelName);
        }

        public virtual IMessageBuilder SetErrorChannel(IMessageChannel errorChannel)
        {
            return SetHeader(MessageHeaders.ERROR_CHANNEL, errorChannel);
        }

        public virtual IMessageBuilder SetErrorChannelName(string errorChannelName)
        {
            return SetHeader(MessageHeaders.ERROR_CHANNEL, errorChannelName);
        }

        public virtual IMessageBuilder SetSequenceNumber(int sequenceNumber)
        {
            return SetHeader(IntegrationMessageHeaderAccessor.SEQUENCE_NUMBER, sequenceNumber);
        }

        public virtual IMessageBuilder SetSequenceSize(int sequenceSize)
        {
            return SetHeader(IntegrationMessageHeaderAccessor.SEQUENCE_SIZE, sequenceSize);
        }

        public virtual IMessageBuilder SetPriority(int priority)
        {
            return SetHeader(IntegrationMessageHeaderAccessor.PRIORITY, priority);
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
            if (_readOnlyHeaders != null)
            {
                foreach (var readOnly in _readOnlyHeaders)
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
}
