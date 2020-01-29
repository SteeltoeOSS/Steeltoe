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

namespace Steeltoe.Common.Converter
{
    /// <summary>
    /// A converter converts from a source object of type S to a target object of type T.
    /// </summary>
    /// <typeparam name="S">type of the source object</typeparam>
    /// <typeparam name="T">type of the target object</typeparam>
    public interface IConverter<in S, out T>
    {
        /// <summary>
        /// Convert the source object of type S to a target object of type T.
        /// </summary>
        /// <param name="source">the source object to convert; can not be null</param>
        /// <returns>the converted object which must be type T</returns>
        T Convert(S source);
    }
}
