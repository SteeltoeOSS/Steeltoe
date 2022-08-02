// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Converter;

public class ConverterNotFoundException : ConversionException
{
    public Type SourceType { get; }

    public Type TargetType { get; }

    public ConverterNotFoundException(Type sourceType, Type targetType)
        : base($"No converter found capable of converting from type [{sourceType}] to type [{targetType}]")
    {
        SourceType = sourceType;
        TargetType = targetType;
    }
}
