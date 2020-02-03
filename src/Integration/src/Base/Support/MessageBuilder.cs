// Copyright 2017 the original author or authors.
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

using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;

namespace Steeltoe.Integration.Support
{
#pragma warning disable SA1402 // File may only contain a single type
    public class MessageBuilder<T> : MessageBuilder, IMessageBuilder<T>
#pragma warning restore SA1402 // File may only contain a single type
    {
        internal MessageBuilder(T payload, IMessage<T> originalMessage)
            : base(payload, originalMessage)
        {
        }

        public new T Payload
        {
            get { return (T)base.Payload; }
        }

        public static MessageBuilder<T> FromMessage(IMessage<T> message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return new MessageBuilder<T>(message.Payload, message);
        }

        public static MessageBuilder<T> WithPayload(T payload)
        {
            return new MessageBuilder<T>(payload, null);
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

        public new IMessageBuilder<T> ReadOnlyHeaders(IList<string> readOnlyHeaders)
        {
            base.ReadOnlyHeaders(readOnlyHeaders);
            return this;
        }

        public new IMessage<T> Build()
        {
            if (!_modified && !_headerAccessor.IsModified && _originalMessage != null
                    && !ContainsReadOnly(_originalMessage.Headers))
            {
                return (IMessage<T>)_originalMessage;
            }

            if (_payload is Exception)
            {
                return (IMessage<T>)new ErrorMessage((Exception)(object)_payload, _headerAccessor.ToDictionary());
            }

            return new GenericMessage<T>((T)_payload, _headerAccessor.ToDictionary());
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

    public class MessageBuilder : AbstractMessageBuilder
    {
        internal MessageBuilder(object payload, IMessage originalMessage)
            : base(payload, originalMessage)
        {
        }

        public override object Payload
        {
            get { return _payload; }
        }

        public override IDictionary<string, object> Headers
        {
            get { return _headerAccessor.ToDictionary(); }
        }

        public static MessageBuilder FromMessage(IMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return new MessageBuilder(message.Payload, message);
        }

        public static MessageBuilder WithPayload(object payload)
        {
            return new MessageBuilder(payload, null);
        }

        public override IMessageBuilder SetHeader(string headerName, object headerValue)
        {
            _headerAccessor.SetHeader(headerName, headerValue);
            return this;
        }

        public override IMessageBuilder SetHeaderIfAbsent(string headerName, object headerValue)
        {
            _headerAccessor.SetHeaderIfAbsent(headerName, headerValue);
            return this;
        }

        public override IMessageBuilder RemoveHeaders(params string[] headerPatterns)
        {
            _headerAccessor.RemoveHeaders(headerPatterns);
            return this;
        }

        public override IMessageBuilder RemoveHeader(string headerName)
        {
            if (!_headerAccessor.IsReadOnly(headerName))
            {
                _headerAccessor.RemoveHeader(headerName);
            }

            return this;
        }

        public override IMessageBuilder CopyHeaders(IDictionary<string, object> headersToCopy)
        {
            _headerAccessor.CopyHeaders(headersToCopy);
            return this;
        }

        public override IMessageBuilder CopyHeadersIfAbsent(IDictionary<string, object> headersToCopy)
        {
            if (headersToCopy != null)
            {
                foreach (var entry in headersToCopy)
                {
                    var headerName = entry.Key;
                    if (!_headerAccessor.IsReadOnly(headerName))
                    {
                        _headerAccessor.SetHeaderIfAbsent(headerName, entry.Value);
                    }
                }
            }

            return this;
        }

        protected override List<List<object>> SequenceDetails
        {
            get { return (List<List<object>>)_headerAccessor.GetHeader(IntegrationMessageHeaderAccessor.SEQUENCE_DETAILS); }
        }

        protected override object CorrelationId
        {
            get { return _headerAccessor.GetCorrelationId(); }
        }

        protected override object SequenceNumber
        {
            get { return _headerAccessor.GetSequenceNumber(); }
        }

        protected override object SequenceSize
        {
            get { return _headerAccessor.GetSequenceSize(); }
        }

        public IMessageBuilder ReadOnlyHeaders(IList<string> readOnlyHeaders)
        {
            _readOnlyHeaders = readOnlyHeaders;
            _headerAccessor.SetReadOnlyHeaders(readOnlyHeaders);
            return this;
        }

        public override IMessage Build()
        {
            if (!_modified && !_headerAccessor.IsModified && _originalMessage != null
                    && !ContainsReadOnly(_originalMessage.Headers))
            {
                return _originalMessage;
            }

            if (_payload is Exception)
            {
                return (IMessage)new ErrorMessage((Exception)(object)_payload, _headerAccessor.ToDictionary());
            }

            return new GenericMessage(_payload, _headerAccessor.ToDictionary());
        }
    }
}
