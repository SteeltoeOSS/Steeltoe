// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace Steeltoe.Common.Converter;

public class StringToCollectionConverter : AbstractToCollectionConverter
{
    private readonly char[] _delimit =
    {
        ','
    };

    public StringToCollectionConverter(IConversionService conversionService)
        : base(GetConvertiblePairs(), conversionService)
    {
    }

    public override bool Matches(Type sourceType, Type targetType)
    {
        if (sourceType == typeof(string) && ConversionUtils.CanCreateCompatListFor(targetType))
        {
            return ConversionUtils.CanConvertElements(sourceType, ConversionUtils.GetElementType(targetType), ConversionService);
        }

        return false;
    }

    public override object Convert(object source, Type sourceType, Type targetType)
    {
        if (source == null)
        {
            return null;
        }

        string sourceString = source as string;
        string[] fields = sourceString.Split(_delimit, StringSplitOptions.RemoveEmptyEntries);
        Type targetElementType = ConversionUtils.GetElementType(targetType);

        IList list = ConversionUtils.CreateCompatListFor(targetType);

        if (list == null)
        {
            throw new InvalidOperationException("Unable to create compatible list");
        }

        foreach (string sourceElement in fields)
        {
            object targetElement = ConversionService.Convert(sourceElement.Trim(), sourceType, targetElementType);
            list.Add(targetElement);
        }

        return list;
    }

    private static ISet<(Type SourceType, Type TargetType)> GetConvertiblePairs()
    {
        return new HashSet<(Type SourceType, Type TargetType)>
        {
            (typeof(string), typeof(ICollection)),
            (typeof(string), typeof(ICollection<>)),
            (typeof(string), typeof(IEnumerable)),
            (typeof(string), typeof(IEnumerable<>))
        };
    }
}
