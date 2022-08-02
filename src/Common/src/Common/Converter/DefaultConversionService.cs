// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Converter;

public class DefaultConversionService : GenericConversionService
{
    private static readonly object Lock = new();

    private static volatile DefaultConversionService _sharedInstance;

    public static IConversionService Singleton
    {
        get
        {
            DefaultConversionService cs = _sharedInstance;

            if (cs == null)
            {
                lock (Lock)
                {
                    cs = _sharedInstance;

                    if (cs == null)
                    {
                        cs = new DefaultConversionService();
                        _sharedInstance = cs;
                    }
                }
            }

            return cs;
        }
    }

    public DefaultConversionService()
    {
        AddDefaultConverters(this);
    }

    public static void AddDefaultConverters(IConverterRegistry converterRegistry)
    {
        AddScalarConverters(converterRegistry);
        AddCollectionConverters(converterRegistry);

        converterRegistry.AddConverter(new ObjectToObjectConverter());

        converterRegistry.AddConverter(new FallbackObjectToStringConverter());
    }

    public static void AddCollectionConverters(IConverterRegistry converterRegistry)
    {
        var conversionService = (IConversionService)converterRegistry;

        converterRegistry.AddConverter(new ListToDictionaryConverter(conversionService));

        converterRegistry.AddConverter(new ArrayToCollectionConverter(conversionService));
        converterRegistry.AddConverter(new CollectionToArrayConverter(conversionService));

        converterRegistry.AddConverter(new ArrayToArrayConverter(conversionService));

        converterRegistry.AddConverter(new CollectionToCollectionConverter(conversionService));

        converterRegistry.AddConverter(new DictionaryToDictionaryConverter(conversionService));
        converterRegistry.AddConverter(new ArrayToStringConverter(conversionService));
        converterRegistry.AddConverter(new StringToArrayConverter(conversionService));

        converterRegistry.AddConverter(new ArrayToObjectConverter(conversionService));
        converterRegistry.AddConverter(new ObjectToArrayConverter(conversionService));

        converterRegistry.AddConverter(new CollectionToStringConverter(conversionService));
        converterRegistry.AddConverter(new StringToCollectionConverter(conversionService));

        converterRegistry.AddConverter(new CollectionToObjectConverter(conversionService));

        converterRegistry.AddConverter(new ObjectToCollectionConverter(conversionService));
    }

    private static void AddScalarConverters(IConverterRegistry converterRegistry)
    {
        converterRegistry.AddConverter(new NumberToNumberConverter());
        converterRegistry.AddConverter(new StringToNumberConverter());

        converterRegistry.AddConverter(new NumberToStringConverter());

        converterRegistry.AddConverter(new StringToCharacterConverter());
        converterRegistry.AddConverter(new CharacterToStringConverter());

        converterRegistry.AddConverter(new NumberToCharacterConverter());
        converterRegistry.AddConverter(new CharacterToNumberConverter());

        converterRegistry.AddConverter(new StringToBooleanConverter());
        converterRegistry.AddConverter(new BooleanToStringConverter());

        converterRegistry.AddConverter(new StringToEnumConverter());
        converterRegistry.AddConverter(new EnumToStringConverter());

        converterRegistry.AddConverter(new StringToEncodingConverter());
        converterRegistry.AddConverter(new EncodingToStringConverter());

        converterRegistry.AddConverter(new StringToGuidConverter());
        converterRegistry.AddConverter(new GuidToStringConverter());

        converterRegistry.AddConverter(new ObjectToNumberConverter());
    }
}
