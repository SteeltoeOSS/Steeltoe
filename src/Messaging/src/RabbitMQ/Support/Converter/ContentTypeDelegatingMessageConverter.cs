// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Rabbit.Extensions;
using System;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Rabbit.Support.Converter
{
    public class ContentTypeDelegatingMessageConverter : ISmartMessageConverter
    {
        public const string DEFAULT_SERVICE_NAME = nameof(ContentTypeDelegatingMessageConverter);

        private readonly Dictionary<string, ISmartMessageConverter> _delegates;

        private readonly ISmartMessageConverter _defaultConverter;

        public string ServiceName { get; set; } = DEFAULT_SERVICE_NAME;

        public ContentTypeDelegatingMessageConverter()
        : this(new Dictionary<string, ISmartMessageConverter>(), new SimpleMessageConverter())
        {
        }

        public ContentTypeDelegatingMessageConverter(ISmartMessageConverter defaultConverter)
            : this(new Dictionary<string, ISmartMessageConverter>(), defaultConverter)
        {
        }

        public ContentTypeDelegatingMessageConverter(Dictionary<string, ISmartMessageConverter> delegates, ISmartMessageConverter defaultConverter)
        {
            _delegates = delegates;
            _defaultConverter = defaultConverter;
        }

        public void AddDelegate(string contentType, ISmartMessageConverter messageConverter)
        {
            _delegates[contentType] = messageConverter;
        }

        public ISmartMessageConverter RemoveDelegate(string contentType)
        {
            _delegates.Remove(contentType, out var removed);
            return removed;
        }

        public object FromMessage(IMessage message, Type targetClass, object conversionHint)
        {
            var contentType = message.Headers.ContentType();
            return GetConverterForContentType(contentType).FromMessage(message, targetClass, conversionHint);
        }

        public T FromMessage<T>(IMessage message, object conversionHint)
        {
            return (T)FromMessage(message, typeof(T), null);
        }

        public object FromMessage(IMessage message, Type targetClass)
        {
            return FromMessage(message, targetClass, null);
        }

        public T FromMessage<T>(IMessage message)
        {
            return (T)FromMessage(message, typeof(T), null);
        }

        public IMessage ToMessage(object payload, IMessageHeaders headers, object conversionHint)
        {
            var contentType = headers.ContentType();
            return GetConverterForContentType(contentType).ToMessage(payload, headers, conversionHint);
        }

        public IMessage ToMessage(object payload, IMessageHeaders headers)
        {
            return ToMessage(payload, headers, null);
        }

        protected virtual ISmartMessageConverter GetConverterForContentType(string contentType)
        {
            ISmartMessageConverter d = null;

            if (contentType != null)
            {
                _delegates.TryGetValue(contentType, out d);
            }

            if (d == null)
            {
                d = _defaultConverter;
            }

            if (d == null)
            {
                throw new MessageConversionException("No delegate converter is specified for content type " + contentType);
            }

            return d;
        }
    }
}
