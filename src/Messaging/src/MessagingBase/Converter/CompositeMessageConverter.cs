// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.Converter;

public class CompositeMessageConverter : ISmartMessageConverter
{
    public const string DefaultServiceName = nameof(CompositeMessageConverter);

    public CompositeMessageConverter(ICollection<IMessageConverter> converters)
    {
        if (converters == null || converters.Count == 0)
        {
            throw new ArgumentException(nameof(converters));
        }

        Converters = new List<IMessageConverter>(converters);
    }

    public string ServiceName { get; set; } = DefaultServiceName;

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
            var result = converter is ISmartMessageConverter smartConverter
                ? smartConverter.FromMessage(message, targetClass, conversionHint)
                : converter.FromMessage(message, targetClass);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    public T FromMessage<T>(IMessage message, object conversionHint) => (T)FromMessage(message, typeof(T), conversionHint);

    public T FromMessage<T>(IMessage message) => (T)FromMessage(message, typeof(T), null);

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
            var result = converter is ISmartMessageConverter smartConverter
                ? smartConverter.ToMessage(payload, headers, conversionHint)
                : converter.ToMessage(payload, headers);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    public List<IMessageConverter> Converters { get; }

    public override string ToString() => $"CompositeMessageConverter[converters={Converters.Count}]";
}
