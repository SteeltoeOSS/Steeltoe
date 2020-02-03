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

namespace Steeltoe.Messaging.Core
{
    /// <summary>
    /// A registry which can be used to store and lookup objects (destinations) by name
    /// </summary>
    public interface IDestinationRegistry : IDisposable
    {
        /// <summary>
        /// Lookup an object by name
        /// </summary>
        /// <param name="name">the name to use during lookup</param>
        /// <returns>the object if found</returns>
        object Lookup(string name);

        /// <summary>
        /// Register an object (destination) in the registry
        /// </summary>
        /// <param name="name">the name of the object</param>
        /// <param name="destination">the object to be associated with the name</param>
        void Register(string name, object destination);

        /// <summary>
        /// Remove a registered object from the registry
        /// </summary>
        /// <param name="name">the name to remove</param>
        /// <returns>the object removed or null if not found</returns>
        object Deregister(string name);

        /// <summary>
        /// Determine if an object exists in the registry
        /// </summary>
        /// <param name="name">the name to use during the lookup</param>
        /// <returns>true if name exists, false otherwise</returns>
        bool Contains(string name);

        /// <summary>
        /// Gets the underlying service provider the registry may be using during name lookup
        /// </summary>
        IServiceProvider ServiceProvider { get; }
    }
}
