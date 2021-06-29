// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging.Converter;
using Steeltoe.Stream.Config;
using System.Collections.Generic;

namespace Steeltoe.Stream.Converter
{
    public class CompositeMessageConverterFactory : IMessageConverterFactory
    {
        private readonly IList<IMessageConverter> _converters;

        public CompositeMessageConverterFactory()
            : this(null)
        {
        }

        public CompositeMessageConverterFactory(IEnumerable<IMessageConverter> converters)
        {
            if (converters == null)
            {
                _converters = new List<IMessageConverter>();
            }
            else
            {
                _converters = new List<IMessageConverter>(converters);
            }

            InitDefaultConverters();

            var resolver = new DefaultContentTypeResolver();
            resolver.DefaultMimeType = BindingOptions.DEFAULT_CONTENT_TYPE;
            foreach (var mc in _converters)
            {
                if (mc is AbstractMessageConverter)
                {
                    ((AbstractMessageConverter)mc).ContentTypeResolver = resolver;
                }
            }
        }

        public IMessageConverter GetMessageConverterForType(MimeType mimeType)
        {
            var converters = new List<IMessageConverter>();
            foreach (var converter in _converters)
            {
                if (converter is AbstractMessageConverter)
                {
                    foreach (var type in ((AbstractMessageConverter)converter).SupportedMimeTypes)
                    {
                        if (type.Includes(mimeType))
                        {
                            converters.Add(converter);
                        }
                    }
                }
            }

            if (converters.Count == 0)
            {
                throw new ConversionException("No message converter is registered for " + mimeType.ToString());
            }

            if (converters.Count > 1)
            {
                return new CompositeMessageConverter(converters);
            }
            else
            {
                return converters[0];
            }
        }

        public ISmartMessageConverter MessageConverterForAllRegistered
        {
            get { return new CompositeMessageConverter(new List<IMessageConverter>(_converters)); }
        }

        public IList<IMessageConverter> AllRegistered => _converters;

        private void InitDefaultConverters()
        {
            var applicationJsonConverter = new ApplicationJsonMessageMarshallingConverter();

            _converters.Add(applicationJsonConverter);

            // TODO: TupleJsonConverter????
            _converters.Add(new ObjectSupportingByteArrayMessageConverter());
            _converters.Add(new ObjectStringMessageConverter());
        }
    }
}
