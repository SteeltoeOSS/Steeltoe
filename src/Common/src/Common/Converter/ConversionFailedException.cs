// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Common.Converter;

public class ConversionFailedException : ConversionException
{
    public ConversionFailedException(Type sourceType, Type targetType, object value, Exception cause)
        : base($"Failed to convert from type [{sourceType}] to type [{targetType}] for value '{(value == null ? "null" : value.ToString())}'", cause)
    {
        SourceType = sourceType;
        TargetType = targetType;
        Value = value;
    }

    public Type SourceType { get; }

    public Type TargetType { get; }

    public object Value { get; }
}
