// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Converter;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;

namespace Steeltoe.Integration.Support.Converter;

public class DefaultDataTypeChannelMessageConverter : IMessageConverter
{
    public const string DefaultServiceName = nameof(DefaultDataTypeChannelMessageConverter);

    private readonly IConversionService _conversionService;

    public string ServiceName { get; set; } = DefaultServiceName;

    public DefaultDataTypeChannelMessageConverter(IConversionService conversionService = null)
    {
        _conversionService = conversionService ?? DefaultConversionService.Singleton;
    }

    public object FromMessage(IMessage message, Type targetType)
    {
        if (_conversionService.CanConvert(message.Payload.GetType(), targetType))
        {
            return _conversionService.Convert(message.Payload, message.Payload.GetType(), targetType);
        }

        return null;
    }

    public T FromMessage<T>(IMessage message)
    {
        return (T)FromMessage(message, typeof(T));
    }

    public IMessage ToMessage(object payload, IMessageHeaders headers)
    {
        throw new NotImplementedException("This converter does not support this method");
    }
}
