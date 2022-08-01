// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Converter;

/// <summary>
///  Allows a converter to conditionally execute based on the types of the source and target.
/// </summary>
public interface IConditionalConverter
{
    /// <summary>
    /// Can the conversion from source type to target type be performed by this converter.
    /// </summary>
    /// <param name="sourceType">the type of the source object to convert from.</param>
    /// <param name="targetType">the type of the target object to convert to.</param>
    /// <returns>true if the conversion can be performed.</returns>
    bool Matches(Type sourceType, Type targetType);
}
