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

namespace Steeltoe.Messaging.Converter
{
    public class CompositeMessageConverter : ISmartMessageConverter
    {
        public const string DEFAULT_SERVICE_NAME = nameof(CompositeMessageConverter);

        public CompositeMessageConverter(ICollection<IMessageConverter> converters)
        {
            if (converters == null || converters.Count == 0)
            {
                throw new ArgumentException(nameof(converters));
            }

            Converters = new List<IMessageConverter>(converters);
        }

        public string ServiceName { get; set; } = DEFAULT_SERVICE_NAME;

        public object FromMessage(IMessage message, Type targetClass)
        {
            foreach (var converter in Converters)
            {
                var result = converter.FromMessage(message, targetClass);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public object FromMessage(IMessage message, Type targetClass, object conversionHint)
        {
            foreach (var converter in Converters)
            {
                var result = converter is ISmartMessageConverter ?
                    ((ISmartMessageConverter)converter).FromMessage(message, targetClass, conversionHint) :
                    converter.FromMessage(message, targetClass);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public T FromMessage<T>(IMessage message, object conversionHint)
        {
            return (T)FromMessage(message, typeof(T), conversionHint);
        }

        public T FromMessage<T>(IMessage message)
        {
            return (T)FromMessage(message, typeof(T), null);
        }

        public IMessage ToMessage(object payload, IMessageHeaders headers)
        {
            foreach (var converter in Converters)
            {
                var result = converter.ToMessage(payload, headers);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public IMessage ToMessage(object payload, IMessageHeaders headers, object conversionHint)
        {
            foreach (var converter in Converters)
            {
                var result = converter is ISmartMessageConverter ?
                         ((ISmartMessageConverter)converter).ToMessage(payload, headers, conversionHint) :
                         converter.ToMessage(payload, headers);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public List<IMessageConverter> Converters { get; }

        public override string ToString()
        {
            return "CompositeMessageConverter[converters=" + Converters.Count + "]";
        }
    }
}
