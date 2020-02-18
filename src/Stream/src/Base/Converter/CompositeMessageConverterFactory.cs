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

        public IMessageConverter MessageConverterForAllRegistered
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
