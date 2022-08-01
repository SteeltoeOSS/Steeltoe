// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging.Converter;
using Steeltoe.Stream.Config;

namespace Steeltoe.Stream.Converter;

public class CompositeMessageConverterFactory : IMessageConverterFactory
{
    public CompositeMessageConverterFactory()
        : this(null)
    {
    }

    public CompositeMessageConverterFactory(IEnumerable<IMessageConverter> converters)
    {
        AllRegistered = converters == null ? new List<IMessageConverter>() : new List<IMessageConverter>(converters);

        InitDefaultConverters();

        var resolver = new DefaultContentTypeResolver { DefaultMimeType = BindingOptions.DefaultContentType };
        foreach (var mc in AllRegistered)
        {
            if (mc is AbstractMessageConverter converter)
            {
                converter.ContentTypeResolver = resolver;
            }
        }
    }

    public IMessageConverter GetMessageConverterForType(MimeType mimeType)
    {
        var converters = new List<IMessageConverter>();
        foreach (var converter in AllRegistered)
        {
            if (converter is AbstractMessageConverter abstractMessageConverter)
            {
                foreach (var type in abstractMessageConverter.SupportedMimeTypes)
                {
                    if (type.Includes(mimeType))
                    {
                        converters.Add(converter);
                    }
                }
            }
        }

        return converters.Count switch
        {
            0 => throw new ConversionException($"No message converter is registered for {mimeType}"),
            > 1 => new CompositeMessageConverter(converters),
            _ => converters[0],
        };
    }

    public ISmartMessageConverter MessageConverterForAllRegistered => new CompositeMessageConverter(new List<IMessageConverter>(AllRegistered));

    public IList<IMessageConverter> AllRegistered { get; }

    private void InitDefaultConverters()
    {
        var applicationJsonConverter = new ApplicationJsonMessageMarshallingConverter();

        AllRegistered.Add(applicationJsonConverter);

        // TODO: TupleJsonConverter????
        AllRegistered.Add(new ObjectSupportingByteArrayMessageConverter());
        AllRegistered.Add(new ObjectStringMessageConverter());
    }
}
