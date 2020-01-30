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

using Steeltoe.Common.Util;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Messaging.Converter
{
    public abstract class AbstractMessageConverter : ISmartMessageConverter
    {
        private readonly List<MimeType> _supportedMimeTypes;

        private IContentTypeResolver _contentTypeResolver = new DefaultContentTypeResolver();

        private bool _strictContentTypeMatch = false;

        private Type _serializedPayloadClass = typeof(byte[]);

        protected AbstractMessageConverter(MimeType supportedMimeType)
        {
            if (supportedMimeType == null)
            {
                throw new ArgumentNullException(nameof(supportedMimeType));
            }

            _supportedMimeTypes = new List<MimeType>() { supportedMimeType };
        }

        protected AbstractMessageConverter(ICollection<MimeType> supportedMimeTypes)
        {
            if (supportedMimeTypes == null)
            {
                throw new ArgumentNullException(nameof(supportedMimeTypes));
            }

            _supportedMimeTypes = new List<MimeType>(supportedMimeTypes);
        }

        public virtual ICollection<MimeType> SupportedMimeTypes
        {
            get { return new List<MimeType>(_supportedMimeTypes); }
        }

        public virtual IContentTypeResolver ContentTypeResolver
        {
            get { return _contentTypeResolver; }
            set { _contentTypeResolver = value; }
        }

        public virtual bool StrictContentTypeMatch
        {
            get
            {
                return _strictContentTypeMatch;
            }

            set
            {
                if (value)
                {
                    if (SupportedMimeTypes.Count <= 0)
                    {
                        throw new InvalidOperationException("Strict match requires non-empty list of supported mime types");
                    }

                    if (ContentTypeResolver == null)
                    {
                        throw new InvalidOperationException("Strict match requires ContentTypeResolver");
                    }
                }

                _strictContentTypeMatch = value;
            }
        }

        public virtual Type SerializedPayloadClass
        {
            get
            {
                return _serializedPayloadClass;
            }

            set
            {
                if (value == typeof(byte[]) || value == typeof(string))
                {
                    _serializedPayloadClass = value;
                }
                else
                {
                    throw new ArgumentException("Payload class must be byte[] or String");
                }
            }
        }

        public virtual T FromMessage<T>(IMessage message)
        {
            return (T)FromMessage(message, typeof(T), null);
        }

        public virtual T FromMessage<T>(IMessage message, object conversionHint = null)
        {
            return (T)FromMessage(message, typeof(T), null);
        }

        public virtual object FromMessage(IMessage message, Type targetClass)
        {
            return FromMessage(message, targetClass, null);
        }

        public virtual object FromMessage(IMessage message, Type targetClass, object conversionHint = null)
        {
            if (!CanConvertFrom(message, targetClass))
            {
                return null;
            }

            return ConvertFromInternal(message, targetClass, conversionHint);
        }

        public virtual bool CanConvertFrom(IMessage message, Type targetClass)
        {
            return Supports(targetClass) && SupportsMimeType(message.Headers);
        }

        public virtual IMessage ToMessage(object payload, IMessageHeaders headers = null)
        {
            return ToMessage(payload, headers, null);
        }

        public virtual IMessage ToMessage(object payload, IMessageHeaders headers = null, object conversionHint = null)
        {
            if (!CanConvertTo(payload, headers))
            {
                return null;
            }

            var payloadToUse = ConvertToInternal(payload, headers, conversionHint);
            if (payloadToUse == null)
            {
                return null;
            }

            var mimeType = GetDefaultContentType(payloadToUse);
            if (headers != null)
            {
                var accessor = MessageHeaderAccessor.GetAccessor<MessageHeaderAccessor>(headers, typeof(MessageHeaderAccessor));
                if (accessor != null && accessor.IsMutable)
                {
                    if (mimeType != null)
                    {
                        accessor.SetHeaderIfAbsent(MessageHeaders.CONTENT_TYPE, mimeType);
                    }

                    return MessageBuilder<object>.CreateMessage(payloadToUse, accessor.MessageHeaders);
                }
            }

            var builder = MessageBuilder<object>.WithPayload(payloadToUse);
            if (headers != null)
            {
                builder.CopyHeaders(headers);
            }

            if (mimeType != null)
            {
                builder.SetHeaderIfAbsent(MessageHeaders.CONTENT_TYPE, mimeType);
            }

            return builder.Build();
        }

        protected virtual MimeType GetDefaultContentType(object payload)
        {
            var mimeTypes = SupportedMimeTypes;
            return mimeTypes.SingleOrDefault();
        }

        protected virtual bool CanConvertTo(object payload, IMessageHeaders headers = null)
        {
            return Supports(payload.GetType()) && SupportsMimeType(headers);
        }

        protected virtual bool SupportsMimeType(IMessageHeaders headers)
        {
            if (SupportedMimeTypes.Count == 0)
            {
                return true;
            }

            var mimeType = GetMimeType(headers);
            if (mimeType == null)
            {
                return !StrictContentTypeMatch;
            }

            foreach (var current in SupportedMimeTypes)
            {
                if (current.Type.Equals(mimeType.Type) && current.Subtype.Equals(mimeType.Subtype))
                {
                    return true;
                }
            }

            return false;
        }

        protected virtual MimeType GetMimeType(IMessageHeaders headers)
        {
            return headers != null && _contentTypeResolver != null ? _contentTypeResolver.Resolve(headers) : null;
        }

        protected abstract bool Supports(Type clazz);

        protected virtual object ConvertFromInternal(IMessage message, Type targetClass, object conversionHint)
        {
            return null;
        }

        protected virtual object ConvertToInternal(object payload, IMessageHeaders headers, object conversionHint)
        {
            return null;
        }
    }
}
