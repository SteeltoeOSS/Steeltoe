// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Converter
{
    public class CompositeMessageConverter : ISmartMessageConverter
    {
        public CompositeMessageConverter(ICollection<IMessageConverter> converters)
        {
            if (converters == null || converters.Count == 0)
            {
                throw new ArgumentException(nameof(converters));
            }

            Converters = new List<IMessageConverter>(converters);
        }

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

        public object FromMessage(IMessage message, Type targetClass, object conversionHint = null)
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

        public T FromMessage<T>(IMessage message, object conversionHint = null)
        {
            return (T)FromMessage(message, typeof(T), conversionHint);
        }

        public T FromMessage<T>(IMessage message)
        {
            return (T)FromMessage(message, typeof(T), null);
        }

        public IMessage ToMessage(object payload, IMessageHeaders headers = null)
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

        public IMessage ToMessage(object payload, IMessageHeaders headers = null, object conversionHint = null)
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
