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
    ///  Allows a converter to conditionally execute based on the types of the source and target
    /// </summary>
    public interface IConditionalConverter
    {
        /// <summary>
        /// Can the conversion from source type to target type be performed by this converter
        /// </summary>
        /// <param name="sourceType">the type of the source object to convert from</param>
        /// <param name="targetType">the type of the target object to conver to</param>
        /// <returns>true if the conversion can be performed</returns>
        bool Matches(Type sourceType, Type targetType);
    }
}
