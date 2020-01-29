// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;

namespace Steeltoe.Common.Converter
{
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
}
