// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Converter
{
    /// <summary>
    /// Generic converter interface for converting between two or more types
    /// </summary>
    public interface IGenericConverter
    {
        /// <summary>
        /// Gets the source and target types this converter can convert between.
        /// </summary>
        ISet<(Type Source, Type Target)> ConvertibleTypes { get; }

        /// <summary>
        /// Convert the source object to the target type.
        /// </summary>
        /// <param name="source">the object to convert; the source can be null</param>
        /// <param name="sourceType">the type of the source that should be used during the conversion.
        /// Can be null, and defaults to the type of the source object</param>
        /// <param name="targetType">the type we are converting to</param>
        /// <returns>the converted object</returns>
        object Convert(object source, Type sourceType, Type targetType);
    }
}
