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

namespace Steeltoe.Messaging.Core
{
    /// <summary>
    /// Strategy for resolving a string name to a destination
    /// </summary>
    public interface IDestinationResolver
    {
        /// <summary>
        /// Resolve the name to a destination
        /// </summary>
        /// <param name="name">the name to resolve</param>
        /// <returns>the destination if it exists</returns>
        object ResolveDestination(string name);
    }

    /// <summary>
    /// A typed strategy for resolving a string name to a destination
    /// </summary>
    /// <typeparam name="T">the type of destinations this resolver returns</typeparam>
    public interface IDestinationResolver<out T> : IDestinationResolver
    {
        /// <summary>
        /// Resolve the name to a destination
        /// </summary>
        /// <param name="name">the name to resolve</param>
        /// <returns>the destination if it exists</returns>
        new T ResolveDestination(string name);
    }
}
