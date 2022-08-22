// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Converter;

public class ConversionFailedException : ConversionException
{
    public Type SourceType { get; }
    public Type TargetType { get; }
    public object Value { get; }

    public ConversionFailedException(Type sourceType, Type targetType, object value, Exception innerException)
        : base($"Failed to convert from type [{sourceType}] to type [{targetType}] for value '{(value == null ? "null" : value.ToString())}'", innerException)
    {
        SourceType = sourceType;
        TargetType = targetType;
        Value = value;
    }
}
