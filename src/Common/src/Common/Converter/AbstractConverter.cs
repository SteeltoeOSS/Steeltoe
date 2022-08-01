// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Converter;

public abstract class AbstractConverter<TSource, TTarget> : AbstractGenericConditionalConverter, IConverter<TSource, TTarget>
{
    protected AbstractConverter()
        : base(new HashSet<(Type Source, Type Target)> { (typeof(TSource), typeof(TTarget)) })
    {
    }

    public override bool Matches(Type sourceType, Type targetType) => typeof(TTarget) == targetType;

    public abstract TTarget Convert(TSource source);

    public override object Convert(object source, Type sourceType, Type targetType)
        => source switch
        {
            null => null,
            not TSource => throw new ArgumentException("'source' type invalid"),
            _ => Convert((TSource)source)
        };
}
