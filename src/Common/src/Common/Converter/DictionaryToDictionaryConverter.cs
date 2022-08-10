// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace Steeltoe.Common.Converter;

public class DictionaryToDictionaryConverter : AbstractToCollectionConverter
{
    public DictionaryToDictionaryConverter(IConversionService conversionService)
        : base(GetConvertiblePairs(), conversionService)
    {
    }

    public override bool Matches(Type sourceType, Type targetType)
    {
        return ConversionUtils.CanCreateCompatDictionaryFor(targetType) && CanConvertKey(sourceType, targetType) && CanConvertValue(sourceType, targetType);
    }

    public override object Convert(object source, Type sourceType, Type targetType)
    {
        if (source == null)
        {
            return null;
        }

        var sourceDict = source as IDictionary;
        int len = sourceDict.Count;

        // Shortcut if possible...
        bool copyRequired = !targetType.IsInstanceOfType(source);

        if (!copyRequired && len == 0)
        {
            return source;
        }

        Type targetKeyType = ConversionUtils.GetDictionaryKeyType(targetType);
        Type targetValueType = ConversionUtils.GetDictionaryValueType(targetType);
        Type sourceKeyType = ConversionUtils.GetDictionaryKeyType(sourceType);
        Type sourceValueType = ConversionUtils.GetDictionaryValueType(sourceType);

        IDictionary dict = ConversionUtils.CreateCompatDictionaryFor(targetType);

        if (dict == null)
        {
            throw new InvalidOperationException("Unable to create compatible dictionary");
        }

        if (!targetType.IsGenericType)
        {
            IDictionaryEnumerator enumerator = sourceDict.GetEnumerator();

            while (enumerator.MoveNext())
            {
                dict.Add(enumerator.Key, enumerator.Value);
            }
        }
        else
        {
            IDictionaryEnumerator enumerator = sourceDict.GetEnumerator();

            while (enumerator.MoveNext())
            {
                object targetKey = ConvertKey(enumerator.Key, sourceKeyType, targetKeyType);
                object targetValue = ConvertValue(enumerator.Value, sourceValueType, targetValueType);
                dict.Add(targetKey, targetValue);
            }
        }

        return dict;
    }

    private static ISet<(Type SourceType, Type TargetType)> GetConvertiblePairs()
    {
        return new HashSet<(Type SourceType, Type TargetType)>
        {
            (typeof(IDictionary), typeof(IDictionary)),
            (typeof(IDictionary), typeof(IDictionary<,>)),
            (typeof(IDictionary), typeof(Dictionary<,>))
        };
    }

    private bool CanConvertKey(Type sourceType, Type targetType)
    {
        return ConversionUtils.CanConvertElements(ConversionUtils.GetDictionaryKeyType(sourceType), ConversionUtils.GetDictionaryKeyType(targetType),
            ConversionService);
    }

    private bool CanConvertValue(Type sourceType, Type targetType)
    {
        return ConversionUtils.CanConvertElements(ConversionUtils.GetDictionaryValueType(sourceType), ConversionUtils.GetDictionaryValueType(targetType),
            ConversionService);
    }

    private object ConvertKey(object sourceKey, Type sourceType, Type targetType)
    {
        if (targetType == null)
        {
            return sourceKey;
        }

        return ConversionService.Convert(sourceKey, sourceKey.GetType(), targetType);
    }

    private object ConvertValue(object sourceValue, Type sourceType, Type targetType)
    {
        if (targetType == null)
        {
            return sourceValue;
        }

        return ConversionService.Convert(sourceValue, sourceValue != null ? sourceValue.GetType() : sourceType, targetType);
    }
}
