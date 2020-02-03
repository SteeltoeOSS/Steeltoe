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

using System;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Support
{
#pragma warning disable SA1402 // File may only contain a single type
    public class MessageBuilder<T> : MessageBuilder
#pragma warning restore SA1402 // File may only contain a single type
    {
        protected new T payload;

        protected MessageBuilder(IMessage<T> originalMessage)
            : base(originalMessage)
        {
            payload = originalMessage.Payload;
        }

        protected MessageBuilder(T payload, MessageHeaderAccessor accessor)
            : base(accessor)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            this.payload = payload;
        }

        public static MessageBuilder<T> FromMessage(IMessage<T> message)
        {
            return new MessageBuilder<T>(message);
        }

        public static MessageBuilder<T> WithPayload(T payload)
        {
            return new MessageBuilder<T>(payload, new MessageHeaderAccessor());
        }

        public static IMessage<T> CreateMessage(T payload, IMessageHeaders messageHeaders)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            if (messageHeaders == null)
            {
                throw new ArgumentNullException(nameof(messageHeaders));
            }

            if (payload is Exception)
            {
                return (IMessage<T>)new ErrorMessage((Exception)(object)payload, messageHeaders);
            }
            else
            {
                return new GenericMessage<T>(payload, messageHeaders);
            }
        }

        public new MessageBuilder<T> SetHeaders(MessageHeaderAccessor accessor)
        {
            base.SetHeaders(accessor);
            return this;
        }

        public new MessageBuilder<T> SetHeader(string headerName, object headerValue)
        {
            base.SetHeader(headerName, headerValue);
            return this;
        }

        public new MessageBuilder<T> SetHeaderIfAbsent(string headerName, object headerValue)
        {
            base.SetHeaderIfAbsent(headerName, headerValue);
            return this;
        }

        public new MessageBuilder<T> RemoveHeaders(params string[] headerPatterns)
        {
            base.RemoveHeaders(headerPatterns);
            return this;
        }

        public new MessageBuilder<T> RemoveHeader(string headerName)
        {
            base.RemoveHeader(headerName);
            return this;
        }

        public new MessageBuilder<T> CopyHeaders(IDictionary<string, object> headersToCopy)
        {
            base.CopyHeaders(headersToCopy);
            return this;
        }

        public new MessageBuilder<T> CopyHeadersIfAbsent(IDictionary<string, object> headersToCopy)
        {
            base.CopyHeadersIfAbsent(headersToCopy);
            return this;
        }

        public new MessageBuilder<T> SetReplyChannel(IMessageChannel replyChannel)
        {
            headerAccessor.ReplyChannel = replyChannel;
            return this;
        }

        public new MessageBuilder<T> SetReplyChannelName(string replyChannelName)
        {
            base.SetReplyChannelName(replyChannelName);
            return this;
        }

        public new MessageBuilder<T> SetErrorChannel(IMessageChannel errorChannel)
        {
            base.SetErrorChannel(errorChannel);
            return this;
        }

        public new MessageBuilder<T> SetErrorChannelName(string errorChannelName)
        {
            base.SetErrorChannelName(errorChannelName);
            return this;
        }

        public new IMessage<T> Build()
        {
            if (originalMessage != null && !headerAccessor.IsModified)
            {
                return (IMessage<T>)originalMessage;
            }

            var headersToUse = headerAccessor.ToMessageHeaders();
            return CreateMessage(payload, headersToUse);
        }
    }

    public class MessageBuilder
    {
        protected readonly object payload;

        protected IMessage originalMessage;

        protected MessageHeaderAccessor headerAccessor;

        protected MessageBuilder()
        {
        }

        protected MessageBuilder(IMessage originalMessage)
        {
            if (originalMessage == null)
            {
                throw new ArgumentNullException(nameof(originalMessage));
            }

            payload = originalMessage.Payload;
            this.originalMessage = originalMessage;
            headerAccessor = new MessageHeaderAccessor(originalMessage);
        }

        protected MessageBuilder(MessageHeaderAccessor accessor)
        {
            if (accessor == null)
            {
                throw new ArgumentNullException(nameof(accessor));
            }

            originalMessage = null;
            headerAccessor = accessor;
        }

        protected MessageBuilder(object payload, MessageHeaderAccessor accessor)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            if (accessor == null)
            {
                throw new ArgumentNullException(nameof(accessor));
            }

            this.payload = payload;
            originalMessage = null;
            headerAccessor = accessor;
        }

        public static MessageBuilder FromMessage(IMessage message)
        {
            return new MessageBuilder(message);
        }

        public static MessageBuilder WithPayload(object payload)
        {
            return new MessageBuilder(payload, new MessageHeaderAccessor());
        }

        public static IMessage CreateMessage(object payload, IMessageHeaders messageHeaders, Type payloadType = null)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            if (messageHeaders == null)
            {
                throw new ArgumentNullException(nameof(messageHeaders));
            }

            if (payload is Exception)
            {
                return (IMessage)new ErrorMessage((Exception)(object)payload, messageHeaders);
            }
            else
            {
                if (payloadType == null)
                {
                    payloadType = payload.GetType();
                }

                var messageType = typeof(GenericMessage<>).MakeGenericType(payloadType);
                return (IMessage)Activator.CreateInstance(messageType, payload, messageHeaders);
            }
        }

        public virtual MessageBuilder SetHeaders(MessageHeaderAccessor accessor)
        {
            if (accessor == null)
            {
                throw new ArgumentNullException(nameof(accessor));
            }

            headerAccessor = accessor;
            return this;
        }

        public virtual MessageBuilder SetHeader(string headerName, object headerValue)
        {
            headerAccessor.SetHeader(headerName, headerValue);
            return this;
        }

        public virtual MessageBuilder SetHeaderIfAbsent(string headerName, object headerValue)
        {
            headerAccessor.SetHeaderIfAbsent(headerName, headerValue);
            return this;
        }

        public virtual MessageBuilder RemoveHeaders(params string[] headerPatterns)
        {
            headerAccessor.RemoveHeaders(headerPatterns);
            return this;
        }

        public virtual MessageBuilder RemoveHeader(string headerName)
        {
            headerAccessor.RemoveHeader(headerName);
            return this;
        }

        public virtual MessageBuilder CopyHeaders(IDictionary<string, object> headersToCopy)
        {
            headerAccessor.CopyHeaders(headersToCopy);
            return this;
        }

        public virtual MessageBuilder CopyHeadersIfAbsent(IDictionary<string, object> headersToCopy)
        {
            headerAccessor.CopyHeadersIfAbsent(headersToCopy);
            return this;
        }

        public virtual MessageBuilder SetReplyChannel(IMessageChannel replyChannel)
        {
            headerAccessor.ReplyChannel = replyChannel;
            return this;
        }

        public virtual MessageBuilder SetReplyChannelName(string replyChannelName)
        {
            headerAccessor.ReplyChannelName = replyChannelName;
            return this;
        }

        public virtual MessageBuilder SetErrorChannel(IMessageChannel errorChannel)
        {
            headerAccessor.ErrorChannel = errorChannel;
            return this;
        }

        public virtual MessageBuilder SetErrorChannelName(string errorChannelName)
        {
            headerAccessor.ErrorChannelName = errorChannelName;
            return this;
        }

        public virtual IMessage Build()
        {
            if (originalMessage != null && !headerAccessor.IsModified)
            {
                return originalMessage;
            }

            var headersToUse = headerAccessor.ToMessageHeaders();
            return CreateMessage(payload, headersToUse);
        }
    }
}
