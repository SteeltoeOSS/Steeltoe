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

using Steeltoe.Common.Converter;
using Steeltoe.Messaging.Converter;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Integration.Support.Converter
{
    public class ConfigurableCompositeMessageConverter : CompositeMessageConverter
    {
        private readonly bool _registerDefaults;
        private IConversionService _conversionService;

        public ConfigurableCompositeMessageConverter(IConversionService conversionService = null)
        : base(InitDefaults())
        {
            _registerDefaults = true;
            _conversionService = conversionService;
            AfterPropertiesSet();
        }

        public ConfigurableCompositeMessageConverter(IMessageConverterFactory factory, IConversionService conversionService = null)
            : this(factory.AllRegistered, false, conversionService)
        {
        }

        public ConfigurableCompositeMessageConverter(IEnumerable<IMessageConverter> converters, bool registerDefaults, IConversionService conversionService = null)
            : base(registerDefaults ? InitDefaults(converters) : converters.ToList())
        {
            _registerDefaults = registerDefaults;
            _conversionService = conversionService;
            AfterPropertiesSet();
        }

        protected void AfterPropertiesSet()
        {
            if (_registerDefaults)
            {
                if (_conversionService == null)
                {
                    _conversionService = DefaultConversionService.Singleton;
                }

                Converters.Add(new GenericMessageConverter(_conversionService));
            }
        }

        private static ICollection<IMessageConverter> InitDefaults(IEnumerable<IMessageConverter> extras = null)
        {
            var converters = extras != null ? new List<IMessageConverter>(extras) : new List<IMessageConverter>();

            converters.Add(new NewtonJsonMessageConverter());
            converters.Add(new ByteArrayMessageConverter());
            converters.Add(new ObjectStringMessageConverter());

            // TODO do we port it together with MessageConverterUtils ?
            // converters.add(new JavaSerializationMessageConverter());
            converters.Add(new GenericMessageConverter());

            return converters;
        }
    }
}
