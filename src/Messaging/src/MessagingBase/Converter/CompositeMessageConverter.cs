// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.Converter;

public class CompositeMessageConverter : ISmartMessageConverter
{
    public const string DefaultServiceName = nameof(CompositeMessageConverter);

    public string ServiceName { get; set; } = DefaultServiceName;

    public List<IMessageConverter> Converters { get; }

    public CompositeMessageConverter(ICollection<IMessageConverter> converters)
    {
        if (converters == null || converters.Count == 0)
        {
            throw new ArgumentException(nameof(converters));
        }

        Converters = new List<IMessageConverter>(converters);
    }

    public object FromMessage(IMessage message, Type targetType)
    {
        foreach (IMessageConverter converter in Converters)
        {
            object result = converter.FromMessage(message, targetType);

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    public object FromMessage(IMessage message, Type targetType, object conversionHint)
    {
        foreach (IMessageConverter converter in Converters)
        {
            object result = converter is ISmartMessageConverter smartConverter
                ? smartConverter.FromMessage(message, targetType, conversionHint)
                : converter.FromMessage(message, targetType);

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
        foreach (IMessageConverter converter in Converters)
        {
            IMessage result = converter.ToMessage(payload, headers);

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    public IMessage ToMessage(object payload, IMessageHeaders headers, object conversionHint)
    {
        foreach (IMessageConverter converter in Converters)
        {
            IMessage result = converter is ISmartMessageConverter smartConverter
                ? smartConverter.ToMessage(payload, headers, conversionHint)
                : converter.ToMessage(payload, headers);

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    public override string ToString()
    {
        return $"CompositeMessageConverter[converters={Converters.Count}]";
    }
}
