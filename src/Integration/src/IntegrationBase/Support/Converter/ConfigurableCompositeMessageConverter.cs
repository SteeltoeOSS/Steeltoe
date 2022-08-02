// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Converter;
using Steeltoe.Messaging.Converter;

namespace Steeltoe.Integration.Support.Converter;

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
            _conversionService ??= DefaultConversionService.Singleton;

            Converters.Add(new GenericMessageConverter(_conversionService));
        }
    }

    private static ICollection<IMessageConverter> InitDefaults(IEnumerable<IMessageConverter> extras = null)
    {
        List<IMessageConverter> converters = extras != null ? new List<IMessageConverter>(extras) : new List<IMessageConverter>();

        converters.Add(new NewtonJsonMessageConverter());
        converters.Add(new ByteArrayMessageConverter());
        converters.Add(new ObjectStringMessageConverter());

        // TODO do we port it together with MessageConverterUtils ?
        // converters.add(new JavaSerializationMessageConverter());
        converters.Add(new GenericMessageConverter());

        return converters;
    }
}
