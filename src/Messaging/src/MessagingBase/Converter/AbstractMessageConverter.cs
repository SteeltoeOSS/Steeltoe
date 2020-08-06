﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

        public abstract string ServiceName { get; set; }

        public virtual T FromMessage<T>(IMessage message)
        {
            return (T)FromMessage(message, typeof(T), null);
        }

        public virtual T FromMessage<T>(IMessage message, object conversionHint)
        {
            return (T)FromMessage(message, typeof(T), null);
        }

        public virtual object FromMessage(IMessage message, Type targetClass)
        {
            return FromMessage(message, targetClass, null);
        }

        public virtual object FromMessage(IMessage message, Type targetClass, object conversionHint)
        {
            if (!CanConvertFrom(message, targetClass))
            {
                return null;
            }

            return ConvertFromInternal(message, targetClass, conversionHint);
        }

        public virtual IMessage ToMessage(object payload, IMessageHeaders headers)
        {
            return ToMessage(payload, headers, null);
        }

        public virtual IMessage ToMessage(object payload, IMessageHeaders headers, object conversionHint)
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
                var accessor = MessageHeaderAccessor.GetAccessor(headers, typeof(MessageHeaderAccessor));
                if (accessor != null && accessor.IsMutable)
                {
                    if (mimeType != null)
                    {
                        accessor.SetHeaderIfAbsent(MessageHeaders.CONTENT_TYPE, mimeType);
                    }

                    return MessageBuilder.CreateMessage(payloadToUse, accessor.MessageHeaders);
                }
            }

            var builder = MessageBuilder.WithPayload(payloadToUse);
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

        public virtual bool CanConvertFrom(IMessage message, Type targetClass)
        {
            return Supports(targetClass) && SupportsMimeType(message.Headers);
        }

        public virtual bool CanConvertTo(object payload, IMessageHeaders headers = null)
        {
            return Supports(payload.GetType()) && SupportsMimeType(headers);
        }

        protected virtual MimeType GetDefaultContentType(object payload)
        {
            var mimeTypes = SupportedMimeTypes;
            return mimeTypes.ElementAt(0);
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
