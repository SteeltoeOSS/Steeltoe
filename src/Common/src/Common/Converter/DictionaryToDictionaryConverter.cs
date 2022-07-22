// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Steeltoe.Common.Converter;

public class DictionaryToDictionaryConverter : AbstractToCollectionConverter
{
    public DictionaryToDictionaryConverter(IConversionService conversionService)
        : base(GetConvertiblePairs(), conversionService)
    {
    }

    public override bool Matches(Type sourceType, Type targetType)
    {
        return ConversionUtils.CanCreateCompatDictionaryFor(targetType) &&
               CanConvertKey(sourceType, targetType) &&
               CanConvertValue(sourceType, targetType);
    }

    public override object Convert(object source, Type sourceType, Type targetType)
    {
        if (source == null)
        {
            return null;
        }

        var sourceDict = source as IDictionary;
        var len = sourceDict.Count;

        // Shortcut if possible...
        var copyRequired = !targetType.IsInstanceOfType(source);
        if (!copyRequired && len == 0)
        {
            return source;
        }

        var targetKeyType = ConversionUtils.GetDictionaryKeyType(targetType);
        var targetValueType = ConversionUtils.GetDictionaryValueType(targetType);
        var sourceKeyType = ConversionUtils.GetDictionaryKeyType(sourceType);
        var sourceValueType = ConversionUtils.GetDictionaryValueType(sourceType);

        var dict = ConversionUtils.CreateCompatDictionaryFor(targetType);
        if (dict == null)
        {
            throw new InvalidOperationException("Unable to create compatable dictionary");
        }

        if (!targetType.IsGenericType)
        {
            var enumerator = sourceDict.GetEnumerator();
            while (enumerator.MoveNext())
            {
                dict.Add(enumerator.Key, enumerator.Value);
            }
        }
        else
        {
            var enumerator = sourceDict.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var targetKey = ConvertKey(enumerator.Key, sourceKeyType, targetKeyType);
                var targetValue = ConvertValue(enumerator.Value, sourceValueType, targetValueType);
                dict.Add(targetKey, targetValue);
            }
        }

        return dict;
    }

    private static ISet<(Type Source, Type Target)> GetConvertiblePairs()
    {
        return new HashSet<(Type Source, Type Target)>()
        {
            (typeof(IDictionary), typeof(IDictionary)),
            (typeof(IDictionary), typeof(IDictionary<,>)),
            (typeof(IDictionary), typeof(Dictionary<,>)),
        };
    }

    private bool CanConvertKey(Type sourceType, Type targetType)
    {
        return ConversionUtils.CanConvertElements(
            ConversionUtils.GetDictionaryKeyType(sourceType),
            ConversionUtils.GetDictionaryKeyType(targetType),
            _conversionService);
    }

    private bool CanConvertValue(Type sourceType, Type targetType)
    {
        return ConversionUtils.CanConvertElements(
            ConversionUtils.GetDictionaryValueType(sourceType),
            ConversionUtils.GetDictionaryValueType(targetType),
            _conversionService);
    }

    private object ConvertKey(object sourceKey, Type sourceType, Type targetType)
    {
        if (targetType == null)
        {
            return sourceKey;
        }

        return _conversionService.Convert(sourceKey, sourceKey.GetType(), targetType);
    }

    private object ConvertValue(object sourceValue, Type sourceType, Type targetType)
    {
        if (targetType == null)
        {
            return sourceValue;
        }

        return _conversionService.Convert(sourceValue, (sourceValue != null) ? sourceValue.GetType() : sourceType, targetType);
    }
}