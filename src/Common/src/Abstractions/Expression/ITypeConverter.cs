// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Converter;
using System;

namespace Steeltoe.Common.Expression.Internal;

/// <summary>
/// A type converter can convert values between different types encountered during expression
/// evaluation.
/// TODO:  This interface is not complete.
/// </summary>
public interface ITypeConverter
{
    public IConversionService ConversionService { get; set; }

    bool CanConvert(Type sourceType, Type targetType);

    object ConvertValue(object value, Type sourceType, Type targetType);
}
