// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Common.Converter;

/// <summary>
/// A service interface for type conversions.
/// </summary>
public interface IConversionService
{
    /// <summary>
    /// Returns true if objects of the source type can be converted to the target type.
    /// </summary>
    /// <param name="sourceType">the type of the source object</param>
    /// <param name="targetType">the type of the target object</param>
    /// <returns>returns true if the conversion can be performed</returns>
    bool CanConvert(Type sourceType, Type targetType);

    /// <summary>
    /// Determine whether the conversion between source type and destination type can be bypassed.
    /// </summary>
    /// <param name="sourceType">the source type</param>
    /// <param name="targetType">the target type</param>
    /// <returns>returns true if it can be bypassed</returns>
    bool CanBypassConvert(Type sourceType, Type targetType);

    /// <summary>
    /// Convert the given source to the target
    /// </summary>
    /// <typeparam name="T">the target type to convert to</typeparam>
    /// <param name="source">the source object to convert</param>
    /// <returns>the converted object</returns>
    T Convert<T>(object source);

    /// <summary>
    /// Convert the given source to the specified target type.
    /// </summary>
    /// <param name="source">the object to convert; may be null</param>
    /// <param name="sourceType">the source objects type</param>
    /// <param name="targetType">the target type to convert to</param>
    /// <returns>the converted object</returns>
    object Convert(object source, Type sourceType, Type targetType);
}